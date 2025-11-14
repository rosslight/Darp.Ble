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
}
