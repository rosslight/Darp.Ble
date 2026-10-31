using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
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
    public async Task CancellingRightAfterTheControllerGotTheAttempt_StillCancelsItThere()
    {
        await using var controller = new FakeController();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        controller.Answer(
            HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            _ =>
            {
                // The controller has the command; its status is not back yet
                cancel.Cancel();
                return [FakeController.CommandStatus(HciOpCode.HCI_LE_Extended_Create_Connection_V1)];
            }
        );
        controller.Answer(
            HciOpCode.HCI_LE_Create_Connection_Cancel,
            _ =>
                [
                    HciMessage.CommandCompleteEventToHost("010E2000"),
                    HciAdapter.ConnectionComplete(0, HciCommandStatus.UnknownConnectionIdentifier),
                ]
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: cancel.Token)
        );

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
    public async Task TwoAttemptsAtOnce_EachGetTheirOwnLink()
    {
        await using var controller = new FakeController();
        BleAddress other = BleAddress.CreateRandomAddress((UInt48)0x223344556677);
        var first = new FakePeripheral(controller, 0x0001) { Mtu = 65 };
        var second = new FakePeripheral(controller, 0x0002) { Mtu = 100 };
        _ = FakePeripheral.ServeAsync(controller, first);
        _ = FakePeripheral.ServeAsync(controller, second);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        Task<ConnectedPeripheral> connectingFirst = adapter.Central.ConnectAsync(
            HciAdapter.Peer,
            cancellationToken: Token
        );
        Task<ConnectedPeripheral> connectingSecond = adapter.Central.ConnectAsync(other, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(HciAdapter.ConnectionComplete(0x0001));
        await using ConnectedPeripheral firstConnection = await connectingFirst;
        // The second attempt starts once the first link is up
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1, occurrence: 2);
        await controller.PushAsync(HciAdapter.ConnectionComplete(0x0002, peer: other));
        await using ConnectedPeripheral secondConnection = await connectingSecond;

        firstConnection.MaxWritePayloadLength.ShouldBe(62);
        secondConnection.MaxWritePayloadLength.ShouldBe(97);
    }

    [Fact]
    public async Task ALostTransportBeforeTheControllerAcceptedTheAttempt_FailsItAtOnce()
    {
        await using var controller = new FakeController();
        controller.Hold(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);

        controller.Fail(new IOException("The serial port is gone"));

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(connecting);
        exception.Kind.ShouldBe(BleConnectErrorKind.Unreachable);
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
    public async Task ADisconnectTheControllerDoesNotAnswer_StillEndsTheConnectionAndItsRequests()
    {
        await using var controller = new FakeController();
        var time = new FakeTimeProvider();
        var peripheral = new FakePeripheral(controller, Handle).AddService(
            0x180F,
            service => service.AddCharacteristic(0x2A19, GattProperty.Read, [80])
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);
        ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic level = (
            await (await connection.DiscoverServiceAsync(0x180F, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        peripheral.HoldAnswers = true;
        controller.Hold(HciOpCode.HCI_Disconnect);
        Task<byte[]> read = level.ReadAsync(Token);
        await peripheral.WaitForReceivedAsync(received =>
            received.Count(pdu => pdu[0] == (byte)AttOpCode.ATT_READ_REQ) == 1
        );

        Task disposing = connection.DisposeAsync().AsTask();
        await controller.WaitForCommandAsync(HciOpCode.HCI_Disconnect);
        time.Advance(TimeSpan.FromSeconds(5));

        await Should.ThrowAsync<BleDisconnectedException>(read);
        await disposing;
        (await connection.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
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
    public async Task ALinkThatGoesDownDuringTheMtuExchange_IsUnreachable()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(HciAdapter.ConnectionComplete(Handle));
        await controller.ReadAclAsync(Token);

        // The peripheral never answered on the new link
        await controller.PushAsync(
            HciMessages.HciDisconnectionCompleteEvent(
                Handle,
                HciCommandStatus.ConnectionFailedToBeEstablishedOrSynchronizationTimeout
            )
        );

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(connecting);
        exception.Kind.ShouldBe(BleConnectErrorKind.Unreachable);
        exception.NativeCode.ShouldBe(0x3E);
        exception.InnerException.ShouldBeOfType<BleDisconnectedException>();
        adapter.Central.Connections.ShouldBeEmpty();
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
