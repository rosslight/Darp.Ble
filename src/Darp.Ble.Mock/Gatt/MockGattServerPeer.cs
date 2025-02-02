using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerPeer(MockBleCentral central, BleAddress address, MockGattClientPeer clientPeer, ILogger<MockGattServerPeer> logger)
    : GattServerPeer(central, address, logger)
{
    private readonly MockGattClientPeer _clientPeer = clientPeer;

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices(this);

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(this, uuid);

    protected override void DisposeCore()
    {
        ConnectionSubject.OnNext(ConnectionStatus.Disconnected);
        base.DisposeCore();
    }
}