using Darp.Ble.Data;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class GattServerPeerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact(Timeout = 5000)]
    public async Task DiscoverServicesAsync_OneServiceEndWithNotFound()
    {
        const ushort connectionHandle = 0x001;
        BleUuid expectedUuid = BleUuid.FromUInt16(0x180D);

        HciMessage[] messages =
        [
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x0001, 0x0005, expectedUuid.ToByteArray())
            ),
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x0006),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);

        peer.Services.Count.ShouldBe(1);
        var service = peer.Services.Single().ShouldBeOfType<HciHostGattServerService>();
        service.Uuid.ShouldBe(expectedUuid);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task DiscoverServicesAsync_MultipleServicesAcrossMultipleResponses()
    {
        const ushort connectionHandle = 0x001;
        BleUuid gapServiceUuid = BleUuid.FromUInt16(0x1800); // GAP Service
        BleUuid service1Uuid = BleUuid.FromUInt16(0x180D); // Heart Rate
        BleUuid service2Uuid = BleUuid.FromUInt16(0x180F); // Battery Service
        BleUuid service3Uuid = BleUuid.FromUInt16(0x180A); // Device Information

        HciMessage[] messages =
        [
            // First response with GAP service and first custom service
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x0001, 0x0005, gapServiceUuid.ToByteArray()),
                new AttGroupTypeData(0x0006, 0x000A, service1Uuid.ToByteArray())
            ),
            // Second response with remaining services (starting from handle 0x000B)
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x000B, 0x000F, service2Uuid.ToByteArray()),
                new AttGroupTypeData(0x0010, 0x0015, service3Uuid.ToByteArray())
            ),
            // End with NotFound error
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x0016),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);

        peer.Services.Count.ShouldBe(4);
        peer.Services[0].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[0].Uuid.ShouldBe(gapServiceUuid);
        peer.Services[1].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[1].Uuid.ShouldBe(service1Uuid);
        peer.Services[2].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[2].Uuid.ShouldBe(service2Uuid);
        peer.Services[3].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[3].Uuid.ShouldBe(service3Uuid);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }
}
