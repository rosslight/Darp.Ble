using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Connections;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> One connection to a peripheral. The core hands it one request at a time </summary>
/// <remarks>
/// An operation of this connection or of the services, characteristics and descriptors it returned fails in one of three ways:
/// <list type="bullet">
/// <item> The link is gone: the driver reports it through <see cref="IConnectionSink.OnDisconnected"/> before it fails the tasks
/// still running, also when it only learns of it from a failed request. Any exception will do; the core ends the connection
/// and the callers get <see cref="BleDisconnectedException"/> </item>
/// <item> The peer answered with an ATT error: <see cref="GattException"/> with the status; the connection stays </item>
/// <item> The platform or radio refused the operation, e.g. the app may not use the device: <see cref="BleException"/>; the
/// connection stays and the caller gets the exception as is </item>
/// </list>
/// Disposing it ends this app's connection and must end every task still running
/// </remarks>
[Experimental(Experiments.Backends)]
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
