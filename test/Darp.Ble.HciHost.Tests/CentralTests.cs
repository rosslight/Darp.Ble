using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class CentralTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact(Timeout = 5000)]
    public async Task ConnectToPeripheral_HappyPath()
    {
        const ushort connectionHandle = 0x0001;
        var peerAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);

        ReplayTransportLayer replay = ReplayTransportLayer.ReplayAfterBleDeviceInitialization([
            HciMessages.HciLeExtendedCreateConnectionCommandStatusEvent(),
            HciMessages.HciLeReadPhyEvent(connectionHandle, txPhy: 0x01, rxPhy: 0x01),
            HciMessages.AttExchangeMtuResponse(connectionHandle, serverRxMtu: 65),
            HciMessages.HciDisconnectionCompleteEvent(connectionHandle),
        ]);

        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replay, token: Token);

        IObservable<IGattServerPeer> connectionObservable = device.Central.ConnectToPeripheral(peerAddress);
        Task<IGattServerPeer> connectionTask = connectionObservable.FirstAsync().ToTask(Token);

        // Wait a short while until the HCI_LE_Extended_Create_Connection_V1 event was propagated
        await Task.Delay(10, Token);

        replay.Push(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                connectionHandle: connectionHandle,
                role: HciLeConnectionRole.Central,
                peerAddressType: (byte)peerAddress.Type,
                peerAddress: peerAddress.Value,
                connectionInterval: 0x0028,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x01F4,
                centralClockAccuracy: 0x01
            )
        );

        var result = (await connectionTask).ShouldBeOfType<HciHostGattServerPeer>();

        result.Address.ShouldBe(peerAddress);
        result.ConnectionHandle.ShouldBe(connectionHandle);

        await result.DisposeAsync();
        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task DisposeDevice_ShouldWork()
    {
        const ushort connectionHandle = 0x001;

        HciMessage[] messages = [HciMessages.HciDisconnectionCompleteEvent(connectionHandle)];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.Central.Device.DisposeAsync();

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task ConnectToPeripheral_ZeroConnectionHandle_DisconnectBeforeMtuResponse_ShouldThrowConnectionInitializationFailedException()
    {
        const ushort connectionHandle = 0x0000;
        var peerAddress = BleAddress.CreateRandomAddress((UInt48)0x5AB482714055);

        var responses = new List<HciMessage>(ReplayTransportLayer.InitializeBleDeviceMessages)
        {
            HciMessages.HciLeExtendedCreateConnectionCommandStatusEvent(),
            HciMessages.HciLeReadPhyEvent(connectionHandle, txPhy: 0x01, rxPhy: 0x01),
        };

        ReplayTransportLayer? replay = null;
        replay = new ReplayTransportLayer(
            (_, i) =>
            {
                if (i >= responses.Count)
                    return (null, TimeSpan.Zero);

                (HciMessage? Message, TimeSpan) response = ReplayTransportLayer.IterateHciMessages(responses, i);
                if (response.Message is { Type: HciPacketType.HciAclData })
                    replay?.Push(HciMessages.HciNumberOfCompletedPacketsEvent(connectionHandle));
                return response;
            },
            ReplayTransportLayer.InitializeBleDeviceMessages.Length,
            logger: null
        );

        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replay, token: Token);

        Task<IGattServerPeer> connectionTask = device.Central.ConnectToPeripheral(peerAddress).FirstAsync().ToTask();

        await Task.Delay(10, Token);
        replay.Push(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                connectionHandle,
                HciLeConnectionRole.Central,
                (byte)peerAddress.Type,
                peerAddress.Value,
                connectionInterval: 0x0018,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x0048,
                centralClockAccuracy: 0x00
            )
        );

        await WaitForOutgoingMessagesAsync(replay, expectedCount: 3, Token);

        replay.Push(
            HciMessages.HciDisconnectionCompleteEvent(
                connectionHandle,
                reason: HciCommandStatus.ConnectionFailedToBeEstablishedOrSynchronizationTimeout
            )
        );

        await WaitForOutgoingMessagesAsync(replay, expectedCount: 3, Token);
        var exception = await Should.ThrowAsync<BleCentralConnectionInitializationFailedException>(async () =>
            await connectionTask
        );
        exception.Address.ShouldBe(peerAddress);
        exception.ConnectionHandle.ShouldBe(connectionHandle);

        var innerException = exception.InnerException.ShouldBeOfType<HciConnectionDisconnectedException>();
        innerException.ConnectionHandle.ShouldBe(connectionHandle);
        innerException.Operation.ShouldBe("ATT query ATT_EXCHANGE_MTU_REQ");
        innerException.DisconnectReason.ShouldBe(
            HciCommandStatus.ConnectionFailedToBeEstablishedOrSynchronizationTimeout
        );

        HciMessage[] messagesToController = replay.MessagesToController.ToArray();
        string expectedCreateConnectionPayload =
            $"43201A0001{(byte)peerAddress.Type:X2}55407182B45A01600060000C0018000000480000000000";

        messagesToController.Length.ShouldBeGreaterThanOrEqualTo(3);
        Convert.ToHexString(messagesToController[0].PduBytes).ShouldBe(expectedCreateConnectionPayload);
        Convert.ToHexString(messagesToController[1].PduBytes).ShouldBe("3020020000");

        HciAclPacket
            .TryReadLittleEndian(messagesToController[2].PduBytes, out HciAclPacket mtuRequestPacket)
            .ShouldBeTrue();
        mtuRequestPacket.ConnectionHandle.ShouldBe(connectionHandle);
        mtuRequestPacket.DataBytes.Span[4].ShouldBe((byte)AttOpCode.ATT_EXCHANGE_MTU_REQ);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    private static async Task WaitForOutgoingMessagesAsync(
        ReplayTransportLayer replay,
        int expectedCount,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested)
        {
            if (replay.MessagesToController.Count >= expectedCount)
                return;

            await Task.Delay(10, token);
        }
    }
}
