using Darp.Ble.Backends;

namespace Darp.Ble.Connections;

/// <summary> The sink of one connection. Holds back a disconnect that arrives before the connection object exists </summary>
internal sealed class ConnectionEvents : IConnectionSink
{
    private readonly Lock _lock = new();
    private ConnectedPeripheral? _connection;
    private DisconnectInfo? _earlyDisconnect;

    public void OnDisconnected(DisconnectInfo info)
    {
        ConnectedPeripheral? connection;
        lock (_lock)
        {
            connection = _connection;
            if (connection is null)
                _earlyDisconnect ??= info;
        }
        connection?.End(info);
    }

    public void Attach(ConnectedPeripheral connection)
    {
        DisconnectInfo? earlyDisconnect;
        lock (_lock)
        {
            _connection = connection;
            earlyDisconnect = _earlyDisconnect;
        }
        if (earlyDisconnect is not null)
            connection.End(earlyDisconnect);
    }
}
