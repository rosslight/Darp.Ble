using System.Buffers.Binary;
using Darp.BinaryObjects;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Connecting to peripherals from an HCI adapter </summary>
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
    public async Task AnAddressWithoutItsType_ConnectsWithTheTypeItWasHeardWith()
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
        // A legacy report from the random address EE:66:19:91:C4:15
        await using (
            IAsyncEnumerator<IGapAdvertisement> scan = adapter.Observer.ScanAsync(Token).GetAsyncEnumerator(Token)
        )
        {
            ValueTask<bool> next = scan.MoveNextAsync();
            await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Set_Extended_Scan_Enable);
            await controller.PushAsync(
                HciMessage.LeEventToHost("0D0110000115C4911966EE0100FF7FB40000FF0000000000000807FF4C0012020000")
            );
            (await next).ShouldBeTrue();
        }
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);

        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(
            BleAddress.Parse("EE:66:19:91:C4:15", provider: null),
            cancellationToken: cancel.Token
        );
        HciMessage command = await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await cancel.CancelAsync();

        // After the opcode and the length: Initiator_Filter_Policy, Own_Address_Type, then Peer_Address_Type
        command.PduBytes[5].ShouldBe((byte)BleAddressType.Random);
        await Should.ThrowAsync<OperationCanceledException>(connecting);
    }

    [Fact]
    public async Task AnAddressWithoutItsType_ThatWasNeverHeard_IsUnreachable()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        BleConnectException exception = await Should.ThrowAsync<BleConnectException>(
            adapter.Central.ConnectAsync(
                BleAddress.Parse("11:22:33:44:55:66", provider: null),
                cancellationToken: Token
            )
        );

        exception.Kind.ShouldBe(BleConnectErrorKind.Unreachable);
        controller.Commands.ShouldNotContain(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
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
        // The command timeout after the link's supervision timeout of 720 ms
        time.Advance(TimeSpan.FromSeconds(5.72));

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
    public async Task APeripheralThatDoesNotSupportTheMtuExchange_IsConnectedWithTheDefaultMtu()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(HciAdapter.ConnectionComplete(Handle));
        await controller.ReadAclAsync(Token);

        await controller.PushAsync(
            HciMessages.AttErrorResponse(
                Handle,
                AttOpCode.ATT_EXCHANGE_MTU_REQ,
                0x0000,
                AttErrorCode.RequestNotSupportedError
            )
        );

        await using ConnectedPeripheral connection = await connecting;
        connection.MaxWritePayloadLength.ShouldBe(20);
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

    [Theory]
    // Read By Group Type and Read By Type with a 128-bit type find nothing, as with a 16-bit one
    [InlineData(
        new byte[]
        {
            0x10,
            0x01,
            0x00,
            0xFF,
            0xFF,
            0xFB,
            0x34,
            0x9B,
            0x5F,
            0x80,
            0x00,
            0x00,
            0x80,
            0x00,
            0x10,
            0x00,
            0x00,
            0x00,
            0x28,
            0x00,
            0x00,
        },
        AttErrorCode.AttributeNotFoundError
    )]
    [InlineData(
        new byte[]
        {
            0x08,
            0x01,
            0x00,
            0xFF,
            0xFF,
            0xFB,
            0x34,
            0x9B,
            0x5F,
            0x80,
            0x00,
            0x00,
            0x80,
            0x00,
            0x10,
            0x00,
            0x00,
            0x03,
            0x28,
            0x00,
            0x00,
        },
        AttErrorCode.AttributeNotFoundError
    )]
    // A type that groups nothing, 16-bit and 128-bit, and a range that ends before it starts
    [InlineData(new byte[] { 0x10, 0x01, 0x00, 0xFF, 0xFF, 0x03, 0x28 }, AttErrorCode.UnsupportedGroupTypeError)]
    [InlineData(
        new byte[]
        {
            0x10,
            0x01,
            0x00,
            0xFF,
            0xFF,
            0xFB,
            0x34,
            0x9B,
            0x5F,
            0x80,
            0x00,
            0x00,
            0x80,
            0x00,
            0x10,
            0x00,
            0x00,
            0x03,
            0x28,
            0x00,
            0x00,
        },
        AttErrorCode.UnsupportedGroupTypeError
    )]
    [InlineData(new byte[] { 0x08, 0x05, 0x00, 0x01, 0x00, 0x03, 0x28 }, AttErrorCode.InvalidHandle)]
    // Read Blob finds no attribute either; Prepare Write and Execute Write are not supported
    [InlineData(new byte[] { 0x0C, 0x03, 0x00, 0x00, 0x00 }, AttErrorCode.InvalidHandle)]
    [InlineData(new byte[] { 0x16, 0x03, 0x00, 0x00, 0x00, 0x01 }, AttErrorCode.RequestNotSupportedError)]
    [InlineData(new byte[] { 0x18, 0x01 }, AttErrorCode.RequestNotSupportedError)]
    public async Task APeripheralsRequest_ThatOurCentralCannotServe_GetsAnError(byte[] request, AttErrorCode expected)
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, Handle);

        await controller.PushAsync(AttToHost(request));

        // A peripheral that gets no answer runs into its ATT timeout
        HciMessage answer = await controller.ReadAclAsync(Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token);
        // ACL header (4), L2CAP header (4), then the error: opcode, the request's opcode, handle, code
        answer.PduBytes[8].ShouldBe((byte)AttOpCode.ATT_ERROR_RSP);
        answer.PduBytes[9].ShouldBe(request[0]);
        answer.PduBytes[^1].ShouldBe((byte)expected);
    }

    /// <summary> An ATT PDU from the peripheral, as raw bytes </summary>
    private static HciMessage AttToHost(byte[] pdu)
    {
        var frame = new byte[4 + pdu.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)pdu.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), 0x0004);
        pdu.CopyTo(frame.AsSpan(4));
        var packet = new HciAclPacket(
            Handle,
            PacketBoundaryFlag.FirstAutoFlushable,
            BroadcastFlag.PointToPoint,
            (ushort)frame.Length,
            frame
        );
        return HciMessage.AclToHost(packet.ToArrayLittleEndian());
    }
}
