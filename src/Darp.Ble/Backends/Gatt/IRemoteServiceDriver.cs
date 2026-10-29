using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A service discovered on a peripheral. Belongs to its connection; the core queues its requests with the connection's </summary>
/// <remarks> Its operations fail as <see cref="IConnectionDriver"/> describes </remarks>
[Experimental(Experiments.Backends)]
public interface IRemoteServiceDriver
{
    /// <summary> The UUID of the service </summary>
    BleUuid Uuid { get; }

    /// <summary> Discovers the characteristics of the service </summary>
    /// <param name="request"> Reports each transaction, e.g. each page; check <see cref="GattRequest.IsAbandoned"/> before the next one </param>
    /// <returns> The characteristics, in handle order </returns>
    Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request);
}
