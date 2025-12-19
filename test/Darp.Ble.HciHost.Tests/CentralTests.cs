using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
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

        ReplayTransportLayer replay = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(
            [
                HciMessages.HciLeExtendedCreateConnectionCommandStatusEvent(),
                HciMessages.HciLeReadPhyEvent(connectionHandle, txPhy: 0x01, rxPhy: 0x01),
                HciMessages.AttExchangeMtuResponse(connectionHandle, serverRxMtu: 65),
                HciMessages.HciDisconnectionCompleteEvent(connectionHandle),
            ]
        );

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
}
