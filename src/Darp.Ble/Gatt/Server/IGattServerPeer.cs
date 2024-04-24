using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The interface defining a gatt server peer </summary>
public interface IGattServerPeer : IAsyncDisposable
{
    /// <summary> The ble address of the service </summary>
    BleAddress Address { get; }
    /// <summary> All discovered services </summary>
    IReadOnlyDictionary<BleUuid, IGattServerService> Services { get; }
    /// <summary> Observe changes in the connection status </summary>
    IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }
    /// <summary> Discover all services of the peer peripheral </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task </returns>
    Task DiscoverServicesAsync(CancellationToken cancellationToken = default);
    /// <summary> Discover a service specific to the given <paramref name="uuid"/> of the peer peripheral </summary>
    /// <param name="uuid"> The uuid to discover </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A connection to the remote service </returns>
    Task<IGattServerService> DiscoverServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default);
}