using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> Receives what centrals do with the adapter's peripheral role. Called on the driver's thread; the core returns at once </summary>
public interface IGattServerSink
{
    /// <summary> A central connected </summary>
    /// <param name="central"> The connection; the same object identifies it in later calls </param>
    void OnCentralConnected(ICentralConnectionDriver central);

    /// <summary> A connection of a central ended, for whatever reason, including a disconnect the core asked for </summary>
    /// <param name="central"> The connection reported by <see cref="OnCentralConnected"/> </param>
    /// <param name="info"> Why it ended </param>
    void OnCentralDisconnected(ICentralConnectionDriver central, DisconnectInfo info);
}
