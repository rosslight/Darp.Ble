using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> Receives the events of one connection. Calls must stay cheap; the core does its work elsewhere </summary>
[Experimental(Experiments.Backends)]
public interface IConnectionSink
{
    /// <summary> Reports that the link ended without this app asking for it </summary>
    /// <param name="info"> Why it ended </param>
    void OnDisconnected(DisconnectInfo info);

    /// <summary> A notification arrived </summary>
    /// <param name="characteristic"> The characteristic, as the connection returned it from discovery </param>
    /// <param name="value"> The value; only valid during the call, the core copies it </param>
    void OnNotification(IRemoteCharacteristicDriver characteristic, ReadOnlySpan<byte> value);
}
