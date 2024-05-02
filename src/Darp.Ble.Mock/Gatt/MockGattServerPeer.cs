using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerPeer(BleAddress address, MockGattClientPeer clientPeer, ILogger? logger)
    : GattServerPeer(address, logger)
{
    private readonly MockGattClientPeer _clientPeer = clientPeer;

    /// <inheritdoc />
    public override IObservable<ConnectionStatus> WhenConnectionStatusChanged => Observable.Empty<ConnectionStatus>();

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices();

    /// <inheritdoc />
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(uuid);
}