using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerPeer(BleAddress address, MockGattClientPeer clientPeer) : GattServerPeer(address)
{
    private readonly MockGattClientPeer _clientPeer = clientPeer;

    /// <inheritdoc />
    public override IObservable<ConnectionStatus> WhenConnectionStatusChanged => Observable.Empty<ConnectionStatus>();

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices();

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(uuid);
}