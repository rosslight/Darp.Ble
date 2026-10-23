using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> Receives the events of one connection. Calls must stay cheap; the core does its work elsewhere </summary>
public interface IConnectionSink
{
    /// <summary> Reports that the link ended without this app asking for it </summary>
    /// <param name="info"> Why it ended </param>
    void OnDisconnected(DisconnectInfo info);
}
