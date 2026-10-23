using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Gatt;
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

    // Set once disposal disconnected every central; a central reported later goes with the driver
    private bool _drained;

    // Publications; disposal waits for them
    private readonly OperationTracker _operations = new();

    internal BlePeripheral(BleAdapter adapter, IPeripheralDriver driver, ILogger logger)
    {
        Adapter = adapter;
        _driver = driver;
        _logger = logger;
        Gatt = new GattServer(driver, _operations);
        driver.Attach(new PeripheralEvents(this));
    }

    /// <summary> The adapter this peripheral belongs to </summary>
    public BleAdapter Adapter { get; }

    /// <summary> The services the adapter hosts </summary>
    public GattServer Gatt { get; }

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
        bool drained;
        lock (_lock)
        {
            closed = _closed;
            drained = _drained;
            // Disposal waits for every central it can still see, also one it rejects
            if (!drained)
                _centrals.Add(connection, central);
        }
        _logger.LogCentralConnected(central.Address);
        // A central that connects while the adapter closes is let go at once; after disposal the driver's own disposal ends it
        if (closed)
            central.End(new DisconnectInfo(DisconnectReason.LocalRequest), takeLinkDown: !drained);
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
        _operations.Close();
        lock (_lock)
            _closed = true;
    }

    /// <summary> Waits for publications in flight, then disconnects every central </summary>
    internal async Task DisposeAsync()
    {
        CloseAdmission();
        await _operations.CloseAndWaitAsync().ConfigureAwait(false);
        while (true)
        {
            ConnectedCentral[] centrals;
            lock (_lock)
            {
                centrals = [.. _centrals.Values];
                if (centrals.Length == 0)
                {
                    _drained = true;
                    return;
                }
            }
            foreach (ConnectedCentral central in centrals)
                await central.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
