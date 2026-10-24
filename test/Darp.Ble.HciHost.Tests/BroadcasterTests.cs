using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Package;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Advertising on an HCI adapter (seam 1 of PR 6) </summary>
public sealed class BroadcasterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly AdvertisingData Data = AdvertisingData.From(Convert.FromHexString("07FF4C0012020000"));

    [Fact]
    public async Task Advertising_ConfiguresAndStartsASet_AndDisposingRemovesIt()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        AdvertisingSet set = await adapter.Broadcaster.StartAdvertisingAsync(
            Data,
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvNonConnInd,
            cancellationToken: Token
        );
        await set.DisposeAsync();

        set.RandomAddress.ShouldBe(HciAdapter.Address);
        await Verifier.Verify(controller.Messages);
    }

    [Fact]
    public async Task EachSet_GetsAFreeHandle()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        AdvertisingSet first = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );
        await using AdvertisingSet second = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );
        await first.DisposeAsync();
        await using AdvertisingSet third = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );

        // The handle is the first parameter of Set Extended Advertising Parameters
        controller
            .Messages.Where(message =>
                message.Direction is HciDirection.HostToController && message.PduBytes[0] == 0x36
            )
            .Select(message => message.PduBytes[3])
            .ShouldBe([0, 1, 0]);
    }

    [Fact]
    public async Task MoreSetsThanTheControllerHas_FailCreating()
    {
        await using var controller = new FakeController();
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Read_Number_Of_Supported_Advertising_Sets, "013B200001");
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using AdvertisingSet first = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );

        await Should.ThrowAsync<BleException>(() =>
            adapter.Broadcaster.CreateAdvertisingSetAsync(Parameters(), Data, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task AdvertisingDataLongerThanTheControllerTakes_IsRejected()
    {
        await using var controller = new FakeController();
        // The controller takes 31 bytes
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH, "013A20001F00");
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        byte[] bytes = new byte[33];
        bytes[0] = 32;
        bytes[1] = 0xFF;
        AdvertisingData data = AdvertisingData.From(bytes);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            adapter.Broadcaster.CreateAdvertisingSetAsync(Parameters(), data, cancellationToken: Token)
        );
    }

    [Fact]
    public async Task ChangingTheDataToMoreThanTheControllerTakes_IsRejected()
    {
        await using var controller = new FakeController();
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH, "013A20001F00");
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );
        byte[] bytes = new byte[33];
        bytes[0] = 32;
        bytes[1] = 0xFF;
        controller.StartRecording();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            set.SetAdvertisingDataAsync(AdvertisingData.From(bytes), Token)
        );
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            set.SetScanResponseDataAsync(AdvertisingData.From(bytes), Token)
        );

        controller.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangingASetToConnectable_NeedsThePeripheralRole()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using AdvertisingSet set = await adapter.Broadcaster.CreateAdvertisingSetAsync(
            Parameters(),
            Data,
            cancellationToken: Token
        );

        await Should.ThrowAsync<NotSupportedException>(() =>
            set.SetAdvertisingParametersAsync(Parameters() with { Type = BleEventType.AdvInd }, Token)
        );
    }

    [Fact]
    public async Task ConnectableAdvertising_NeedsThePeripheralRole()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        await Should.ThrowAsync<NotSupportedException>(() =>
            adapter.Broadcaster.CreateAdvertisingSetAsync(
                Parameters() with
                {
                    Type = BleEventType.AdvInd,
                },
                Data,
                cancellationToken: Token
            )
        );
    }

    private static AdvertisingParameters Parameters() =>
        new()
        {
            Type = BleEventType.AdvNonConnInd,
            MinPrimaryAdvertisingInterval = TimeSpan.FromMilliseconds(100),
            MaxPrimaryAdvertisingInterval = TimeSpan.FromMilliseconds(100),
        };
}
