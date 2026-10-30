using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Diagnostics;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> A connection to a peripheral, from the central side. It ends once; afterwards it cannot be used again </summary>
public sealed class ConnectedPeripheral : IAsyncDisposable
{
    /// <summary> The largest ATT MTU the core uses: the longest attribute value (512) plus a write's header (5) </summary>
    private const int ClientMtu = 517;

    /// <summary> The header of a write request: the opcode and the attribute handle </summary>
    private const int WriteHeaderLength = 3;

    private readonly BleCentral _central;
    private readonly IConnectionDriver _driver;
    private readonly ILogger _logger;

    // Completes when the link is down: at once when the driver reports it lost, else once the driver took it down
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    // Completes after the driver was disposed and the requests ended; disposal of the connection and its adapter waits for it
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly RequestQueue _requests;

    // The address as logs may show it
    private readonly object _peer;
    private readonly DiagnosticsSettings _diagnostics;

    // Set once by the MTU exchange, before the connection is handed out
    private int _maxWritePayloadLength;

    // Guards the fields below: the driver's thread delivers notifications and ends the connection while the app uses it
    private readonly Lock _lock = new();
    private readonly Dictionary<IRemoteCharacteristicDriver, NotificationChannel> _notifications = [];
    private readonly Dictionary<(IRemoteCharacteristicDriver, Type), object> _helpers = [];

    // Why the connection ends, once it started ending; the first reason wins
    private DisconnectInfo? _ending;

    // Whether this connection counts in darp.ble.connection.count; set when it is handed out
    private bool _counted;

    internal ConnectedPeripheral(
        BleCentral central,
        BleAddress address,
        IConnectionDriver driver,
        TimeProvider timeProvider,
        QueueOptions notificationQueue,
        string backend,
        DiagnosticsSettings diagnostics,
        ILogger logger
    )
    {
        Backend = backend;
        _peer = diagnostics.Peer(address);
        _diagnostics = diagnostics;
        NotificationQueue = notificationQueue;
        TimeProvider = timeProvider;
        _central = central;
        _driver = driver;
        _logger = logger;
        // An ATT timeout leaves the link up; the driver's disposal takes it down
        _requests = new RequestQueue(
            address,
            backend,
            diagnostics,
            timeProvider,
            info => End(info, takeLinkDown: true),
            logger
        );
        Address = address;
    }

    /// <summary> The address of the peripheral </summary>
    public BleAddress Address { get; }

    /// <summary> Discovers the primary services of the peripheral </summary>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The services, in the peripheral's order </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    /// <exception cref="BleException"> The platform refused the operation, e.g. it does not let this app use the device; the connection stays open </exception>
    public async Task<IReadOnlyList<RemoteGattService>> DiscoverServicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<IRemoteServiceDriver> services = await RunAsync(
                GattOperation.DiscoverServices,
                _driver.DiscoverServicesAsync,
                cancellationToken
            )
            .ConfigureAwait(false);
        return [.. services.Select(service => new RemoteGattService(this, service))];
    }

    /// <summary> Discovers a primary service of the peripheral </summary>
    /// <param name="uuid"> The UUID of the service </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The first service with the UUID </returns>
    /// <exception cref="GattException"> The peripheral has no such service (<see cref="GattErrorKind.NotFound"/>) or answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    /// <exception cref="BleException"> The platform refused the operation, e.g. it does not let this app use the device; the connection stays open </exception>
    public async Task<RemoteGattService> DiscoverServiceAsync(
        BleUuid uuid,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(uuid);
        IReadOnlyList<RemoteGattService> services = await DiscoverServicesAsync(cancellationToken)
            .ConfigureAwait(false);
        return services.FirstOrDefault(service => service.Uuid == uuid)
            ?? throw new GattException(GattErrorKind.NotFound, $"The peripheral has no service {uuid}");
    }

    /// <summary> The longest value a write can carry, from the MTU agreed when connecting; it may grow later, e.g. on Windows </summary>
    /// <remarks> Longer writes are rejected on every backend; a notification longer than this is cut to it </remarks>
    public int MaxWritePayloadLength => _maxWritePayloadLength;

    /// <summary> Completes once, when the connection ended and its link is down, with why it ended </summary>
    /// <remarks> On a lost link it completes before the requests fail and the streams end; otherwise after them, once the link was taken down </remarks>
    public Task<DisconnectInfo> Disconnected => _disconnected.Task;

    /// <summary> Ends the connection. Calling it again, or after the peripheral disconnected, waits for the same end </summary>
    /// <param name="cancellationToken"> Stops waiting; the connection still ends </param>
    /// <returns> A task that completes once the connection ended and its backend released it </returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        End(new DisconnectInfo(DisconnectReason.LocalRequest), takeLinkDown: true);
        await _released.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Ends the connection; the same as <see cref="DisconnectAsync"/> </summary>
    /// <returns> A task that completes once the connection ended and its backend released it </returns>
    public ValueTask DisposeAsync() => new(DisconnectAsync(CancellationToken.None));

    /// <summary> Rejects a value one write cannot carry; long writes would need prepared writes, which are left out </summary>
    internal void EnsureFitsOneWrite(ReadOnlyMemory<byte> value)
    {
        if (value.Length > _maxWritePayloadLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"A write on this connection carries at most {_maxWritePayloadLength} bytes"
            );
        }
    }

    /// <summary> Runs a request of this connection once the earlier ones ended </summary>
    internal Task<T> RunAsync<T>(
        GattOperation operation,
        Func<GattRequest, Task<T>> send,
        CancellationToken cancellationToken
    ) => _requests.RunAsync(operation, send, cancellationToken);

    /// <summary> Runs a write without response in this connection's send order </summary>
    internal ValueTask RunCommandAsync(
        GattOperation operation,
        IRemoteCharacteristicDriver characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) => _requests.RunCommandAsync(operation, characteristic, value, cancellationToken);

    /// <summary> Runs a request without a result of this connection once the earlier ones ended </summary>
    internal Task RunAsync(
        GattOperation operation,
        Func<GattRequest, Task> send,
        CancellationToken cancellationToken
    ) =>
        _requests.RunAsync(
            operation,
            async request =>
            {
                await send(request).ConfigureAwait(false);
                return true;
            },
            cancellationToken
        );

    /// <summary> The queue of a stream that does not bring its own </summary>
    internal QueueOptions NotificationQueue { get; }

    /// <summary> The backend, for spans and metrics </summary>
    internal string Backend { get; }

    /// <summary> State a library helper shares across wrappers of the same characteristic, e.g. an echo query session </summary>
    internal T GetOrAddHelper<T>(IRemoteCharacteristicDriver characteristic, Func<T> create)
        where T : class
    {
        lock (_lock)
        {
            if (!_helpers.TryGetValue((characteristic, typeof(T)), out object? helper))
            {
                helper = create();
                _helpers.Add((characteristic, typeof(T)), helper);
            }
            return (T)helper;
        }
    }

    /// <summary> The manager's clock, for timeouts of helpers built on this connection </summary>
    internal TimeProvider TimeProvider { get; }

    /// <summary> The notifications of a characteristic; the same channel for the same driver object </summary>
    internal NotificationChannel GetNotifications(IRemoteCharacteristicDriver characteristic)
    {
        NotificationChannel? channel;
        DisconnectInfo? ending;
        lock (_lock)
        {
            ending = _ending;
            if (!_notifications.TryGetValue(characteristic, out channel))
            {
                channel = new NotificationChannel(characteristic, Backend);
                _notifications.Add(characteristic, channel);
            }
        }
        // A channel created after the end ends at once, so its streams end like all others
        if (ending is not null)
            channel.End(ending);
        return channel;
    }

    internal void OnNotification(IRemoteCharacteristicDriver characteristic, ReadOnlySpan<byte> value)
    {
        NotificationChannel? channel;
        lock (_lock)
            _notifications.TryGetValue(characteristic, out channel);
        // Without a channel nobody subscribed: dropped
        channel?.Deliver(value);
    }

    /// <summary> The connection is handed out; it counts as open until it ends. Called under the central's lock </summary>
    internal void CountOpen()
    {
        lock (_lock)
        {
            // A connection that already started ending is never counted, so its teardown has nothing to take back
            if (_ending is not null)
                return;
            _counted = true;
        }
        Telemetry.Connections.Add(1, Tags());
    }

    private System.Diagnostics.TagList Tags() =>
        new() { { Telemetry.BackendTag, Backend }, { Telemetry.RoleTag, "central" } };

    /// <summary> Why the connection ends, once it started ending </summary>
    internal DisconnectInfo? Ending
    {
        get
        {
            lock (_lock)
                return _ending;
        }
    }

    /// <summary> Agrees on the MTU, as the connection's first request; before the connection is handed out </summary>
    internal async Task ExchangeMtuAsync(CancellationToken cancellationToken)
    {
        int mtu = await _requests
            .RunAsync(
                GattOperation.ExchangeMtu,
                request => _driver.ExchangeMtuAsync(ClientMtu, request),
                cancellationToken
            )
            .ConfigureAwait(false);
        GrowMtu(mtu);
    }

    /// <summary> The backend reports a larger MTU than the exchange returned </summary>
    internal void OnMtuChanged(int mtu) => GrowMtu(mtu);

    /// <summary> Keeps the larger MTU: an MTU never shrinks, and the exchange and a change may arrive in either order </summary>
    private void GrowMtu(int mtu)
    {
        lock (_lock)
            _maxWritePayloadLength = Math.Max(_maxWritePayloadLength, mtu - WriteHeaderLength);
    }

    /// <summary> Ends the connection exactly once; the first reason wins </summary>
    /// <param name="info"> Why it ended </param>
    /// <param name="takeLinkDown"> Whether the driver still has to take the link down; not when it reported the end </param>
    internal void End(DisconnectInfo info, bool takeLinkDown)
    {
        bool counted;
        lock (_lock)
        {
            if (_ending is not null)
                return;
            _ending = info;
            counted = _counted;
        }
        // A lost link is down already: code that catches the failures below sees the end everywhere
        if (!takeLinkDown)
            OnLinkDown(info, counted);
        // Requests fail with this end from now on; the ones running end with the driver
        _requests.Close(info);
        // Streams deliver what they queued, then end the same way
        NotificationChannel[] channels;
        lock (_lock)
            channels = [.. _notifications.Values];
        foreach (NotificationChannel channel in channels)
            channel.End(info);
        // Off the caller's thread: a driver may report a disconnect from its read loop, and teardown calls back into it
        _ = Task.Run(() => TeardownAsync(info, takeLinkDown, counted));
    }

    private async Task TeardownAsync(DisconnectInfo info, bool takeLinkDown, bool counted)
    {
        try
        {
            await _requests.WhenHandOffsEnded.ConfigureAwait(false);
            await _driver.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(_peer, _diagnostics.Exception(e));
        }
        finally
        {
            await _requests.DrainAsync().ConfigureAwait(false);
            // The driver's disposal took the link down
            if (takeLinkDown)
                OnLinkDown(info, counted);
            _central.Remove(this);
            _released.TrySetResult();
        }
    }

    /// <summary> The link is down: the connection no longer counts as open and leaves the central's connections </summary>
    private void OnLinkDown(DisconnectInfo info, bool counted)
    {
        if (counted)
            Telemetry.Connections.Add(-1, Tags());
        _logger.LogDisconnected(_peer, info.Reason);
        _disconnected.TrySetResult(info);
    }
}
