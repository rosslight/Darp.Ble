using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerPeer : IAsyncDisposable
{
    BleAddress Address { get; }
    IReadOnlyDictionary<BleUuid, IGattServerService> Services { get; }
    IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }
    Task DiscoverServicesAsync(CancellationToken cancellationToken = default);
    Task<IGattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default);
}