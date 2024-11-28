using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerPeer(MockBleCentral central, BleAddress address, MockGattClientPeer clientPeer, ILogger? logger)
    : GattServerPeer(central, address, logger)
{
    private readonly MockGattClientPeer _clientPeer = clientPeer;

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices();

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(uuid);
}