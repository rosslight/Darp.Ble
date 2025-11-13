using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class PeripheralTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact(Timeout = 5000)]
    public async Task OnHciConnectionCompleteEvent_SuccessfulConnection_ShouldAddPeerAndEmitWhenConnected()
    {
        const ushort connectionHandle = 0x0001;
        var peerAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);

        ReplayTransportLayer replay = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(
            [HciMessages.HciDisconnectionCompleteEvent(connectionHandle)]
        );

        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replay, token: Token);

        // Subscribe to WhenConnected observable before pushing the event
        Task<IGattClientPeer> connectionTask = device.Peripheral.WhenConnected.FirstAsync().ToTask(Token);

        // Push the connection complete event with Peripheral role
        // This will trigger OnHciLeEnhancedConnectionCompleteV1EventPacket which registers the connection,
        // then publishes the message which triggers OnHciConnectionCompleteEvent in the peripheral
        replay.Push(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                connectionHandle: connectionHandle,
                role: HciLeConnectionRole.Peripheral,
                peerAddressType: (byte)peerAddress.Type,
                peerAddress: peerAddress.Value,
                connectionInterval: 0x0028,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x01F4,
                centralClockAccuracy: 0x01
            )
        );

        var clientPeer = (await connectionTask).ShouldBeOfType<HciHostGattClientPeer>();

        clientPeer.Address.ShouldBe(peerAddress);
        clientPeer.ConnectionHandle.ShouldBe(connectionHandle);

        // Verify the peer is in PeerDevices
        device.Peripheral.PeerDevices.ShouldContainKey(peerAddress);
        device.Peripheral.PeerDevices[peerAddress].ShouldBe(clientPeer);

        clientPeer.Dispose();
        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task ServiceDiscovery_WhenCentralRequestsServices_ShouldRespondWithServices()
    {
        const ushort connectionHandle = 0x0001;
        var peerAddress = BleAddress.CreateRandomAddress((UInt48)0x112233445566);

        ReplayTransportLayer replay = ReplayTransportLayer.ReplayAfterBleDeviceInitialization(
            [HciMessages.HciDisconnectionCompleteEvent(connectionHandle)]
        );

        await using IBleDevice device = await Helpers.GetAndInitializeBleDeviceAsync(replay, token: Token);
        GattClientDeviceInformationService service = device.Peripheral.AddDeviceInformationService();

        // Subscribe to WhenConnected observable before pushing the event
        Task<IGattClientPeer> connectionTask = device.Peripheral.WhenConnected.FirstAsync().ToTask(Token);

        // Push the connection complete event with Peripheral role
        replay.Push(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                connectionHandle: connectionHandle,
                role: HciLeConnectionRole.Peripheral,
                peerAddressType: (byte)peerAddress.Type,
                peerAddress: peerAddress.Value,
                connectionInterval: 0x0028,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x01F4,
                centralClockAccuracy: 0x01
            )
        );

        var peer = (await connectionTask).ShouldBeOfType<HciHostGattClientPeer>();

        // Wait a bit for the connection to be fully established
        await Task.Delay(50, Token);

        // Simulate the central sending an ATT Read By Group Type Request to discover services
        // This simulates what a central would send to discover primary services (0x2800)
        var serviceDiscoveryRequest = new AttReadByGroupTypeReq<ushort>
        {
            StartingHandle = 0x0001,
            EndingHandle = 0xFFFF,
            AttributeGroupType = 0x2800, // Primary Service
        };

        replay.Push(HciMessages.AttToHost(connectionHandle, serviceDiscoveryRequest));

        // Wait for the peripheral to process the request and send a response
        await Task.Delay(100, Token);

        // Verify the response was sent
        // The response should be in MessagesToController (sent from host to controller)
        var responses = replay.MessagesToController.Where(m => m.Type == HciPacketType.HciAclData).ToList();

        responses.ShouldNotBeEmpty("Expected at least one ACL response for service discovery");

        // Verify the service is in the peripheral's services collection
        device.Peripheral.Services.ShouldContain(x => x.Uuid == service.Uuid);

        peer.Dispose();
        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }
}
