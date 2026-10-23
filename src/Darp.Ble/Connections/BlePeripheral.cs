using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> The peripheral role of an adapter: centrals connect to it </summary>
public sealed class BlePeripheral
{
    private readonly IPeripheralDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly Dictionary<ICentralConnectionDriver, ConnectedCentral> _centrals = [];
    private bool _closed;

    internal BlePeripheral(BleAdapter adapter, IPeripheralDriver driver, ILogger logger)
    {
        Adapter = adapter;
        _driver = driver;
        _logger = logger;
        driver.Attach(new PeripheralEvents(this));
    }

    /// <summary> The adapter this peripheral belongs to </summary>
    public BleAdapter Adapter { get; }

    /// <summary> The connected centrals, as a snapshot </summary>
    public IReadOnlyList<ConnectedCentral> ConnectedCentrals
    {
        get
        {
            lock (_lock)
                return [.. _centrals.Values];
        }
    }

    internal void OnCentralConnected(ICentralConnectionDriver connection)
    {
        var central = new ConnectedCentral(this, _driver, connection, _logger);
        bool closed;
        lock (_lock)
        {
            closed = _closed;
            if (!closed)
                _centrals.Add(connection, central);
        }
        _logger.LogCentralConnected(central.Address);
        // A central that connects while the adapter closes is let go at once
        if (closed)
            central.End(new DisconnectInfo(DisconnectReason.LocalRequest), takeLinkDown: true);
    }

    internal void OnCentralDisconnected(ICentralConnectionDriver connection, DisconnectInfo info)
    {
        ConnectedCentral? central;
        lock (_lock)
            _centrals.TryGetValue(connection, out central);
        central?.End(info, takeLinkDown: false);
    }

    internal void Remove(ICentralConnectionDriver connection)
    {
        lock (_lock)
            _centrals.Remove(connection);
    }

    internal void CloseAdmission()
    {
        lock (_lock)
            _closed = true;
    }

    /// <summary> Disconnects every central </summary>
    internal async Task DisposeAsync()
    {
        CloseAdmission();
        foreach (ConnectedCentral central in ConnectedCentrals)
            await central.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
