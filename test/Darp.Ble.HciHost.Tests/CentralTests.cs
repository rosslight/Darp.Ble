using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Connecting to peripherals from an HCI adapter (seam 1 of PR 6) </summary>
public sealed class CentralTests
{
    private const ushort Handle = 0x0001;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Connecting_CreatesTheLinkAndAgreesOnTheMtu_AndDisposingTakesTheLinkDown()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle, serverMtu: 65);
        await connection.DisposeAsync();

        connection.MaxWritePayloadLength.ShouldBe(62);
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
        await Verifier.Verify(controller.Messages);
    }

    [Fact]
    public async Task CancellingAnAttempt_CancelsItAtTheController()
    {
        await using var controller = new FakeController();
        controller.Answer(
            HciOpCode.HCI_LE_Create_Connection_Cancel,
            _ =>
                [
                    HciMessage.CommandCompleteEventToHost("010E2000"),
                    HciAdapter.ConnectionComplete(0, HciCommandStatus.UnknownConnectionIdentifier),
                ]
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(
            HciAdapter.Peer,
            cancellationToken: cancel.Token
        );
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);

        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(connecting);
        controller.Commands.ShouldBe([
            HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            HciOpCode.HCI_LE_Create_Connection_Cancel,
        ]);
    }

    [Fact]
    public async Task ALinkThatCameUpWhileCancelling_IsTakenDown()
    {
        await using var controller = new FakeController();
        controller.Answer(
            HciOpCode.HCI_LE_Create_Connection_Cancel,
            _ => [HciAdapter.ConnectionComplete(Handle), HciMessage.CommandCompleteEventToHost("010E20000C")]
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(
            HciAdapter.Peer,
            cancellationToken: cancel.Token
        );
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);

        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(connecting);
        await controller.WaitForCommandAsync(HciOpCode.HCI_Disconnect);
        controller.Commands.ShouldBe([
            HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            HciOpCode.HCI_LE_Create_Connection_Cancel,
            HciOpCode.HCI_Disconnect,
        ]);
    }

    [Fact]
    public async Task AControllerBusyConnecting_RefusesTheAttempt()
    {
        await using var controller = new FakeController();
        controller.Answer(
            HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            _ =>
                [
                    FakeController.CommandStatus(
                        HciOpCode.HCI_LE_Extended_Create_Connection_V1,
                        HciCommandStatus.CommandDisallowed
                    ),
                ]
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(() =>
            adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token)
        );
        exception.Kind.ShouldBe(BleConnectErrorKind.Busy);
        exception.NativeCode.ShouldBe(0x0C);
    }

    [Fact]
    public async Task AnAttemptTheControllerGivesUp_IsUnreachable()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);

        await controller.PushAsync(
            HciAdapter.ConnectionComplete(0, HciCommandStatus.ConnectionFailedToBeEstablishedOrSynchronizationTimeout)
        );

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(connecting);
        exception.Kind.ShouldBe(BleConnectErrorKind.Unreachable);
    }

    [Theory]
    [InlineData(HciCommandStatus.RemoteUserTerminatedConnection, DisconnectReason.RemoteRequest)]
    [InlineData(HciCommandStatus.RemoteDeviceTerminatedConnectionDueToPowerOff, DisconnectReason.RemoteRequest)]
    [InlineData(HciCommandStatus.ConnectionTimeout, DisconnectReason.ConnectionTimeout)]
    public async Task ALinkThatGoesDown_EndsTheConnectionWithItsReason(
        HciCommandStatus reason,
        DisconnectReason expected
    )
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle);

        await controller.PushAsync(HciMessages.HciDisconnectionCompleteEvent(Handle, reason));

        DisconnectInfo info = await connection.Disconnected;
        info.Reason.ShouldBe(expected);
        info.NativeCode.ShouldBe((int)reason);
    }

    [Fact]
    public async Task ALostTransport_FailsAPendingAttempt()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);

        controller.Fail(new IOException("The serial port is gone"));

        await Should.ThrowAsync<BleConnectException>(connecting);
    }

    [Fact]
    public async Task ALostTransport_EndsTheConnection()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle);

        controller.Fail(new IOException("The serial port is gone"));

        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.TransportLost);
    }

    [Fact]
    public async Task ALinkThatGoesDownDuringTheMtuExchange_FailsConnecting()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(HciAdapter.ConnectionComplete(Handle));
        await controller.ReadAclAsync(Token);

        await controller.PushAsync(HciMessages.HciDisconnectionCompleteEvent(Handle));

        await Should.ThrowAsync<BleDisconnectedException>(connecting);
    }

    [Fact]
    public async Task APeripheralAskingForTheMtu_GetsAnAnswer()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle);

        await controller.PushAsync(HciMessages.AttToHost(Handle, new AttExchangeMtuReq { ClientRxMtu = 100 }));

        HciMessage answer = await controller.ReadAclAsync(Token);
        // ACL header (4), L2CAP header (4), then the ATT PDU; the answer tells what this host receives
        answer.PduBytes[8..].ShouldBe([(byte)AttOpCode.ATT_EXCHANGE_MTU_RSP, 0x05, 0x02]);
    }

    [Fact]
    public async Task APeripheralDiscoveringOurServices_FindsNone()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle);

        await controller.PushAsync(
            HciMessages.AttToHost(
                Handle,
                new AttReadByGroupTypeReq<ushort>
                {
                    StartingHandle = 0x0001,
                    EndingHandle = 0xFFFF,
                    AttributeGroupType = 0x2800,
                }
            )
        );

        HciMessage answer = await controller.ReadAclAsync(Token);
        answer.PduBytes[8].ShouldBe((byte)AttOpCode.ATT_ERROR_RSP);
        answer.PduBytes[^1].ShouldBe((byte)AttErrorCode.AttributeNotFoundError);
    }
}
