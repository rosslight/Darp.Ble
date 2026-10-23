using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
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
    private readonly Dictionary<GattAttributeDefinition, IHostedAttribute> _hosted = [];
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
        Gatt = new GattServer(this, driver, _operations);
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

    internal void Host(IHostedAttribute[] attributes)
    {
        lock (_lock)
        {
            foreach (IHostedAttribute attribute in attributes)
            {
                _hosted.Add(attribute.Definition, attribute);
                (attribute as LocalGattCharacteristic)?.Bind(this);
            }
        }
    }

    internal void Unhost(IHostedAttribute[] attributes)
    {
        lock (_lock)
        {
            foreach (IHostedAttribute attribute in attributes)
            {
                _hosted.Remove(attribute.Definition);
                (attribute as LocalGattCharacteristic)?.Unbind();
            }
        }
    }

    internal async Task<byte[]> OnReadRequestAsync(
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition
    )
    {
        (ConnectedCentral central, IHostedAttribute attribute) = Resolve(connection, definition);
        return await central.RunHandlerAsync(ct => attribute.ReadAsync(central, ct)).ConfigureAwait(false);
    }

    internal async Task OnWriteRequestAsync(
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition,
        byte[] value
    )
    {
        (ConnectedCentral central, IHostedAttribute attribute) = Resolve(connection, definition);
        GattProtocolStatus status = await central
            .RunHandlerAsync(ct => attribute.WriteAsync(central, value, ct))
            .ConfigureAwait(false);
        if (status is not GattProtocolStatus.Success)
            throw new GattException(status, $"The write handler answered {status}");
    }

    internal void OnWriteCommand(ICentralConnectionDriver connection, GattAttributeDefinition definition, byte[] value)
    {
        // Nobody waits for the answer; failures are logged by the handler run
        _ = OnWriteRequestAsync(connection, definition, value)
            .ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
    }

    private (ConnectedCentral Central, IHostedAttribute Attribute) Resolve(
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition
    )
    {
        lock (_lock)
        {
            // The driver only reports centrals and characteristics it got from the core
            if (
                !_centrals.TryGetValue(connection, out ConnectedCentral? central)
                || !_hosted.TryGetValue(definition, out IHostedAttribute? attribute)
            )
            {
                throw new GattException(GattProtocolStatus.UnlikelyError, "Unknown central or attribute");
            }
            return (central, attribute);
        }
    }

    /// <summary> Notifies every central that turned notifications on; each independently, failures are logged and skipped </summary>
    internal async ValueTask NotifyAllAsync(
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        ConnectedCentral[] recipients;
        lock (_lock)
            recipients = [.. _centrals.Values];
        // Every central starts before any is awaited, so a slow one delays nobody
        Task[] sends =
        [
            .. recipients
                .Where(central => central.IsSubscribed(characteristic))
                .Select(central => NotifyOneAsync(central, characteristic, value, cancellationToken)),
        ];
        await Task.WhenAll(sends).ConfigureAwait(false);
    }

    private async Task NotifyOneAsync(
        ConnectedCentral central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await central.NotifyAsync(characteristic, value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            // One central's failure is not the caller's; the others still get theirs
            _logger.LogNotifyAllCentralFailed(central.Address, e);
        }
    }

    internal ConnectedCentral? Find(ICentralConnectionDriver connection)
    {
        lock (_lock)
            return _centrals.GetValueOrDefault(connection);
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
            // Every central starts ending before any is awaited, so a slow handler delays nobody else's end
            await Task.WhenAll(centrals.Select(central => central.DisconnectAsync(CancellationToken.None)))
                .ConfigureAwait(false);
        }
    }
}
