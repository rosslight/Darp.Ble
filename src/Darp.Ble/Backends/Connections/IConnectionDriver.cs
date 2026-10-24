namespace Darp.Ble.Backends;

/// <summary> One connection to a peripheral. The core hands it one request at a time </summary>
/// <remarks>
/// When the link goes, the driver reports it through <see cref="IConnectionSink.OnDisconnected"/> before it fails the tasks still running.
/// Disposing it ends this app's connection and must end every task still running
/// </remarks>
public interface IConnectionDriver : IAsyncDisposable
{
    /// <summary> Agrees on the ATT MTU with the peer </summary>
    /// <param name="clientMtu"> The largest MTU the core can use; the driver may offer less </param>
    /// <param name="request"> Reports the transaction and carries the caller's cancellation until it is sent </param>
    /// <returns> The agreed MTU, at least 23 </returns>
    Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request);

    /// <summary> Discovers the primary services of the peer </summary>
    /// <param name="request"> Reports each transaction, e.g. each page; check <see cref="GattRequest.IsAbandoned"/> before the next one </param>
    /// <returns> The services, in handle order </returns>
    Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request);
}
