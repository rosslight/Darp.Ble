using System.Runtime.CompilerServices;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Diagnostics;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> The peripheral role of an adapter: centrals connect to it </summary>
public sealed class BlePeripheral
{
    /// <summary> The Generic Access service, which the backend owns </summary>
    private static readonly BleUuid GapServiceUuid = BleUuid.FromUInt16(0x1800);

    private readonly IPeripheralDriver _driver;
    private readonly TimeProvider _timeProvider;
    private readonly DiagnosticsSettings _diagnostics;
    private readonly ILogger _logger;

    // Publications and identity changes; disposal closes it and waits for them
    private readonly OperationTracker _operations = new();

    // Guards the fields below: the driver's thread reports centrals and their requests while the app adds services
    // and watches centrals
    private readonly Lock _lock = new();

    // In the order the centrals connected, which ConnectedCentrals and WatchCentralsAsync keep
    private readonly OrderedDictionary<ICentralConnectionDriver, ConnectedCentral> _centrals = [];
    private readonly Dictionary<GattAttributeDefinition, IHostedAttribute> _hosted = [];

    // One queue per WatchCentralsAsync loop; written under the lock, so a central is queued in the order it connected
    private readonly List<ItemQueue<ConnectedCentral>> _watchers = [];

    // Set once disposal disconnected every central; a central reported later goes with the driver
    private bool _drained;

    // The GAP identity; changes are serialized and committed once the driver took them
    private readonly Lock _identityLock = new();
    private string _deviceName;
    private AppearanceValues _appearance = AppearanceValues.Unknown;

    internal BlePeripheral(
        BleAdapter adapter,
        IPeripheralDriver driver,
        TimeProvider timeProvider,
        DiagnosticsSettings diagnostics,
        ILogger logger
    )
    {
        Adapter = adapter;
        _timeProvider = timeProvider;
        _diagnostics = diagnostics;
        _driver = driver;
        _logger = logger;
        _deviceName = adapter.Name ?? DefaultDeviceName;
        // Attach comes first, as the driver contract promises; the identity follows before the adapter is handed out
        driver.Attach(new PeripheralEvents(this));
        if (adapter.Capabilities.HasFlag(Capabilities.SetGapIdentity))
            driver.SetGapIdentity(_deviceName, _appearance);
    }

    /// <summary> The adapter this peripheral belongs to </summary>
    public BleAdapter Adapter { get; }

    private const string DefaultDeviceName = "Darp.Ble";

    /// <summary> The device name centrals read from the GAP service; the adapter's name by default </summary>
    /// <exception cref="NotSupportedException"> The backend owns the GAP service; see <see cref="Capabilities.SetGapIdentity"/> </exception>
    public string DeviceName
    {
        get
        {
            EnsureGapIdentity();
            lock (_identityLock)
                return _deviceName;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetIdentity(value, appearance: null);
        }
    }

    /// <summary> The appearance centrals read from the GAP service; <see cref="AppearanceValues.Unknown"/> by default </summary>
    /// <exception cref="NotSupportedException"> The backend owns the GAP service; see <see cref="Capabilities.SetGapIdentity"/> </exception>
    public AppearanceValues Appearance
    {
        get
        {
            EnsureGapIdentity();
            lock (_identityLock)
                return _appearance;
        }
        set => SetIdentity(deviceName: null, value);
    }

    /// <summary> Adds a complete service </summary>
    /// <param name="service"> The service. It cannot change or be added again afterwards, even if adding fails </param>
    /// <param name="cancellationToken"> Cancels adding </param>
    /// <returns> The service, once centrals can discover it </returns>
    /// <exception cref="InvalidOperationException"> The builder's service was already added </exception>
    public async Task<LocalGattService> AddServiceAsync(
        LocalGattServiceBuilder service,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(service);
        if (service.Uuid == GapServiceUuid)
            throw new InvalidOperationException(
                "The backend serves the GAP service; set BlePeripheral.DeviceName and Appearance instead"
            );
        _operations.Enter(typeof(BlePeripheral));
        try
        {
            (GattServiceDefinition definition, IHostedAttribute[] attributes) = service.Build();
            // Known before the driver adds it, so a central can use the service as soon as it is live
            Host(attributes);
            try
            {
                await _driver.AddServiceAsync(definition, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Unhost(attributes);
                throw;
            }
            foreach (LocalGattCharacteristic characteristic in attributes.OfType<LocalGattCharacteristic>())
                characteristic.MarkLive(this);
            return new LocalGattService(definition.Uuid);
        }
        finally
        {
            _operations.Exit();
        }
    }

    /// <summary> The connected centrals, as a snapshot </summary>
    /// <remarks> A central leaves the list once its <see cref="ConnectedCentral.Disconnected"/> completed </remarks>
    public IReadOnlyList<ConnectedCentral> ConnectedCentrals
    {
        get
        {
            // A central stays known until its handlers returned, so disposal can wait for them; for the app it is gone
            lock (_lock)
                return [.. _centrals.Values.Where(central => !central.Disconnected.IsCompleted)];
        }
    }

    /// <summary> The centrals connected now, then each new one once its connection is up; ends when the adapter is disposed </summary>
    /// <remarks>
    /// Each loop has its own queue. A central may send requests before the loop reads it; handlers get the central
    /// themselves. A central that disconnected meanwhile is still yielded
    /// </remarks>
    /// <param name="cancellationToken"> Ends the loop </param>
    /// <returns> The centrals, in the order they connected </returns>
    /// <exception cref="ObjectDisposedException"> The adapter is disposed </exception>
    public async IAsyncEnumerable<ConnectedCentral> WatchCentralsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // Few centrals ever connect, so the queue never needs to drop
        var watcher = new ItemQueue<ConnectedCentral>(QueueOptions.Unbounded, onDrop: () => { });
        lock (_lock)
        {
            // In one step with the snapshot, so no central is missed or yielded twice
            ObjectDisposedException.ThrowIf(_operations.IsClosed, typeof(BlePeripheral));
            foreach (ConnectedCentral central in _centrals.Values.Where(central => !central.Disconnected.IsCompleted))
                watcher.TryWrite(central);
            _watchers.Add(watcher);
        }
        try
        {
            while (await watcher.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (watcher.Reader.TryRead(out ConnectedCentral? central))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return central;
                }
            }
        }
        finally
        {
            lock (_lock)
                _watchers.Remove(watcher);
        }
    }

    internal void OnCentralConnected(ICentralConnectionDriver connection)
    {
        var central = new ConnectedCentral(this, _driver, connection, _timeProvider, _diagnostics, _logger);
        bool closed;
        bool drained;
        lock (_lock)
        {
            // Disposal closes admission before it drains, so a drained peripheral is always closed
            drained = _drained;
            closed = _operations.IsClosed;
            // Disposal waits for every central it can still see, also one it rejects
            if (!drained)
                _centrals.Add(connection, central);
            if (!closed)
            {
                foreach (ItemQueue<ConnectedCentral> watcher in _watchers)
                    watcher.TryWrite(central);
            }
        }
        if (!closed)
            central.CountOpen();
        _logger.LogCentralConnected(central.Peer);
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
                _hosted.Add(attribute.Definition, attribute);
        }
    }

    internal void Unhost(IHostedAttribute[] attributes)
    {
        lock (_lock)
        {
            foreach (IHostedAttribute attribute in attributes)
                _hosted.Remove(attribute.Definition);
        }
    }

    internal async Task<byte[]> OnReadRequestAsync(
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition
    )
    {
        (ConnectedCentral central, IHostedAttribute attribute) = Resolve(connection, definition);
        return await central
            .RunHandlerAsync("read", definition.Uuid, ct => attribute.ReadAsync(central, ct))
            .ConfigureAwait(false);
    }

    internal Task OnWriteRequestAsync(
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition,
        byte[] value
    ) => WriteAsync("write", connection, definition, value);

    private async Task WriteAsync(
        string operation,
        ICentralConnectionDriver connection,
        GattAttributeDefinition definition,
        byte[] value
    )
    {
        (ConnectedCentral central, IHostedAttribute attribute) = Resolve(connection, definition);
        await central
            .RunHandlerAsync(
                operation,
                definition.Uuid,
                async ct =>
                {
                    GattProtocolStatus status = await attribute.WriteAsync(central, value, ct).ConfigureAwait(false);
                    // Inside the run, so its span shows the handler's answer
                    if (status is not GattProtocolStatus.Success)
                        throw new GattException(status, $"The write handler answered {status}");
                    return status;
                }
            )
            .ConfigureAwait(false);
    }

    internal void OnWriteCommand(ICentralConnectionDriver connection, GattAttributeDefinition definition, byte[] value)
    {
        // Nobody waits for the answer; failures are logged by the handler run
        _ = WriteAsync("write_command", connection, definition, value)
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
            _logger.LogNotifyAllCentralFailed(central.Peer, _diagnostics.Exception(e));
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

    /// <summary> Admits nothing new and ends every <see cref="WatchCentralsAsync"/> loop after its queued centrals </summary>
    internal void CloseAdmission()
    {
        _operations.Close();
        // A closed peripheral rejects every central that still connects, so the loops are over
        lock (_lock)
        {
            foreach (ItemQueue<ConnectedCentral> watcher in _watchers)
                watcher.Complete();
        }
    }

    /// <summary> Waits for publications in flight, then disconnects every central and waits until its handlers returned and its states were disposed </summary>
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
            await Task.WhenAll(centrals.Select(central => central.ReleaseAsync())).ConfigureAwait(false);
        }
    }

    private void EnsureGapIdentity()
    {
        if (!Adapter.Capabilities.HasFlag(Capabilities.SetGapIdentity))
            throw new NotSupportedException($"The {Adapter.Backend} backend owns the GAP service");
    }

    /// <summary> Changes the identity; admitted like every operation, so adapter disposal waits for it and rejects it afterwards </summary>
    private void SetIdentity(string? deviceName, AppearanceValues? appearance)
    {
        EnsureGapIdentity();
        _operations.Enter(typeof(BlePeripheral));
        try
        {
            lock (_identityLock)
            {
                string newName = deviceName ?? _deviceName;
                AppearanceValues newAppearance = appearance ?? _appearance;
                _driver.SetGapIdentity(newName, newAppearance);
                _deviceName = newName;
                _appearance = newAppearance;
            }
        }
        finally
        {
            _operations.Exit();
        }
    }
}
