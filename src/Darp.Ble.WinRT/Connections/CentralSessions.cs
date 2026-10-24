using System.Runtime.CompilerServices;

namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// One central per device for as long as its connection lasts, across the GATT sessions Windows hands out. A connection
/// ends when its last session closed or this app ended it; the device's next activity is a new connection, and late
/// callbacks of the old sessions are ignored. Not thread-safe: its owner calls it under its own lock.
/// Generic over the session and central types so the bookkeeping builds and is tested without Windows
/// </summary>
internal sealed class CentralSessions<TSession, TCentral>
    where TSession : class
    where TCentral : class
{
    private readonly Dictionary<string, Connection> _byDevice = new(StringComparer.Ordinal);
    private readonly Dictionary<TSession, Connection> _bySession = new(ReferenceEqualityComparer.Instance);

    // Closed sessions, so their late callbacks do not bring an ended connection back; weak, so they can go away
    private readonly ConditionalWeakTable<TSession, object> _closed = [];

    /// <summary>
    /// The central of the session's device; null for a session that closed. Says whether the central and the session
    /// are new, so each session can be watched for closing and each central announced once
    /// </summary>
    public (TCentral? Central, bool IsNewCentral, bool IsNewSession) Resolve(
        string deviceId,
        TSession session,
        Func<TCentral> create
    )
    {
        if (_closed.TryGetValue(session, out _))
            return (null, false, false);
        bool isNewCentral = !_byDevice.TryGetValue(deviceId, out Connection? connection);
        if (connection is null)
        {
            connection = new Connection(deviceId, create());
            _byDevice[deviceId] = connection;
        }
        bool isNewSession = connection.Sessions.Add(session);
        _bySession[session] = connection;
        return (connection.Central, isNewCentral, isNewSession);
    }

    /// <summary> A session closed; returns the central when that ended its connection </summary>
    public TCentral? Close(TSession session)
    {
        _closed.AddOrUpdate(session, session);
        if (!_bySession.Remove(session, out Connection? connection))
            return null;
        connection.Sessions.Remove(session);
        if (connection.Sessions.Count > 0)
            return null;
        _byDevice.Remove(connection.DeviceId);
        return connection.Central;
    }

    /// <summary> This app ended the central's connection; returns false when it had ended already </summary>
    public bool End(TCentral central)
    {
        Connection? connection = _byDevice.Values.FirstOrDefault(c => ReferenceEquals(c.Central, central));
        if (connection is null)
            return false;
        _byDevice.Remove(connection.DeviceId);
        foreach (TSession session in connection.Sessions)
        {
            _bySession.Remove(session);
            _closed.AddOrUpdate(session, session);
        }
        return true;
    }

    private sealed class Connection(string deviceId, TCentral central)
    {
        public string DeviceId { get; } = deviceId;
        public TCentral Central { get; } = central;
        public HashSet<TSession> Sessions { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
