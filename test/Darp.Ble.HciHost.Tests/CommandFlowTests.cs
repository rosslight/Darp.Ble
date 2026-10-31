using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using SetRandomAddress = Darp.Ble.Hci.Payload.Command.HciLeSetRandomAddressCommand;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Sending commands only while the controller has room for them (Core Vol 4 Part E 4.4) </summary>
public sealed class CommandFlowTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AfterTheControllerReportsNoRoomForCommands_TheNextCommandWaitsUntilItReportsRoomAgain()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        // A successful answer that leaves no room for another command
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Set_Random_Address, "00052000");
        await adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Set_Random_Address, "01052000");
        controller.StartRecording();

        Task second = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        await controller.PushAsync(FakeController.CommandStatus(HciOpCode.None));
        await second;

        controller
            .Messages.Select(message => message.Type)
            .ShouldBe([HciPacketType.HciEvent, HciPacketType.HciCommand, HciPacketType.HciEvent]);
    }

    [Fact]
    public async Task ACommandThatTimedOut_UsedUpItsRoom_SoTheNextOneWaitsUntilTheControllerReportsRoom()
    {
        await using var controller = new FakeController();
        var time = new FakeTimeProvider();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);
        controller.Hold(HciOpCode.HCI_LE_Set_Random_Address);
        Task first = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Random_Address);
        time.Advance(TimeSpan.FromSeconds(5));
        await Should.ThrowAsync<TimeoutException>(first);
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Set_Random_Address, "01052000");

        Task second = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        // The fake transport records commands as they are sent, so this needs no wait
        int sentBeforeRoom = controller.Commands.Count(command => command == HciOpCode.HCI_LE_Set_Random_Address);
        await controller.PushAsync(FakeController.CommandStatus(HciOpCode.None));
        await second;

        sentBeforeRoom.ShouldBe(1);
    }

    [Fact]
    public async Task ALateAnswerToACommandThatTimedOut_GivesRoomBack_ButDoesNotAnswerTheNextCommand()
    {
        await using var controller = new FakeController();
        var time = new FakeTimeProvider();
        await using var device = new HciDevice(controller, 0, HciSettings.Default, time, loggerFactory: null);
        await device.Host.ResetAsync(Token);
        // Room for two: the next command still waits until the controller reports again
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("020000"));
        controller.Hold(HciOpCode.HCI_LE_Set_Random_Address);
        Task first = SetRandomAddressAsync(device, 1);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Random_Address);
        time.Advance(TimeSpan.FromSeconds(5));
        await Should.ThrowAsync<TimeoutException>(first);

        Task<HciLeSetRandomAddressResult> second = SetRandomAddressAsync(device, 2);
        int sentBeforeTheLateAnswer = controller.Commands.Count(command =>
            command == HciOpCode.HCI_LE_Set_Random_Address
        );
        // The first command's answer, Command Disallowed, comes late
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("0105200C"));
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Random_Address, occurrence: 2);
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("01052000"));

        sentBeforeTheLateAnswer.ShouldBe(1);
        (await second).Status.ShouldBe(HciCommandStatus.Success);
    }

    [Fact]
    public async Task ACommandWaitingForRoom_TimesOutLikeAnyCommand()
    {
        await using var controller = new FakeController();
        var time = new FakeTimeProvider();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("000000"));

        Task waiting = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        time.Advance(TimeSpan.FromSeconds(5));

        await Should.ThrowAsync<TimeoutException>(waiting);
        controller.Commands.ShouldNotContain(HciOpCode.HCI_LE_Set_Random_Address);
    }

    [Fact]
    public async Task ATransportFailure_EndsACommandWaitingForRoom()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("000000"));
        Task waiting = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);

        controller.Fail(new IOException("The port is gone"));

        await Should.ThrowAsync<HciException>(waiting);
        controller.Commands.ShouldNotContain(HciOpCode.HCI_LE_Set_Random_Address);
    }

    [Fact]
    public async Task ARefusedCommand_GivesItsRoomBack_OnlyIfTheControllerDidNotReportSince()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        controller.Answer(
            HciOpCode.HCI_LE_Set_Random_Address,
            _ =>
            {
                // The controller reports no room after the command took it, before the transport refuses the command
                controller.PushAsync(HciMessage.CommandCompleteEventToHost("000000")).GetAwaiter().GetResult();
                throw new IOException("The port is busy");
            }
        );
        await Should.ThrowAsync<IOException>(() =>
            adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token)
        );
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Set_Random_Address, "01052000");
        controller.StartRecording();

        Task next = adapter.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress(), Token);
        int sentBeforeRoom = controller.Commands.Count;
        await controller.PushAsync(FakeController.CommandStatus(HciOpCode.None));
        await next;

        sentBeforeRoom.ShouldBe(0);
    }

    [Fact]
    public async Task AnEventThatOnlyGivesRoomForCommands_DoesNotAnswerTheCommandInFlight()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        controller.Hold(HciOpCode.HCI_LE_Set_Random_Address);
        var address = BleAddress.CreateRandomAddress((UInt48)0xC1C2C3C4C5C6);
        Task setting = adapter.SetRandomAddressAsync(address, Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Random_Address);

        // One credit, opcode 0x0000
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("010000"));
        await controller.PushAsync(HciMessage.CommandCompleteEventToHost("01052000"));
        await setting;

        adapter.RandomAddress.ShouldBe(address);
    }

    private static Task<HciLeSetRandomAddressResult> SetRandomAddressAsync(
        HciDevice device,
        Hci.Payload.Command.UInt48 address
    ) =>
        device.Host.QueryCommandCompletionAsync<SetRandomAddress, HciLeSetRandomAddressResult>(
            new SetRandomAddress(address),
            timeout: null,
            Token
        );
}
