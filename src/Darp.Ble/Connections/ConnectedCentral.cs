using System.Diagnostics;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Diagnostics;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> A central connected to the adapter's peripheral role. It ends once; afterwards it cannot be used again </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The app does not own a central; its connection's end releases them once the handlers returned"
)]
public sealed class ConnectedCentral
{
    private readonly BlePeripheral _peripheral;
    private readonly IPeripheralDriver _driver;
    private readonly ICentralConnectionDriver _connection;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly DiagnosticsSettings _diagnostics;

    // Whether this connection counts in darp.ble.connection.count; set when the peripheral takes it in
    private bool _counted;

    // Warns once when handlers still run 5 s after the connection started ending (D9)
    private ITimer? _slowHandlers;

    // Completes once the handlers admitted before the end returned; independent of the driver's teardown
    private Task _handlersReturned = Task.CompletedTask;
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private DisconnectInfo? _ending;

    // State of the app for this connection (D9). Its own lock: a slow factory must not block the driver's thread,
    // which takes _lock to end the connection
    private readonly Lock _stateLock = new();
    private readonly Dictionary<Type, object> _states = [];
    private bool _statesSealed;

    // The characteristics this central turned notifications on for; empty for good once the connection ends
    private readonly HashSet<GattCharacteristicDefinition> _subscriptions = [];

    // Hosted handlers of this central: one at a time, cancelled and awaited when the connection ends (D9)
    private readonly SemaphoreSlim _handlerTurn = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly OperationTracker _handlers = new();

    // Notifications to this central, sent one after another in call order (D8); the connection's end cancels and awaits them
    private readonly OperationTracker _notifications = new();
    private readonly SemaphoreSlim _notifyTurn = new(1, 1);

    internal ConnectedCentral(
        BlePeripheral peripheral,
        IPeripheralDriver driver,
        ICentralConnectionDriver connection,
        TimeProvider timeProvider,
        DiagnosticsSettings diagnostics,
        ILogger logger
    )
    {
        _peripheral = peripheral;
        _driver = driver;
        _connection = connection;
        _timeProvider = timeProvider;
        _logger = logger;
        Address = connection.Address;
        Peer = diagnostics.Peer(connection.Address);
        _diagnostics = diagnostics;
    }

    /// <summary> The address of the central </summary>
    public BleAddress Address { get; }

    /// <summary> Completes once, when the connection ended, with why it ended </summary>
    public Task<DisconnectInfo> Disconnected => _disconnected.Task;

    /// <summary> Ends the connection. Calling it again, or after the central disconnected, waits for the same end </summary>
    /// <param name="cancellationToken"> Stops waiting; the connection still ends </param>
    /// <returns> A task that completes once the connection ended </returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        End(new DisconnectInfo(DisconnectReason.LocalRequest), takeLinkDown: true);
        await _disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> The address as logs and spans may show it </summary>
    internal object Peer { get; }

    /// <summary> The peripheral took the connection in; it counts as open until it ends </summary>
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

    /// <summary> The peripheral role this central is connected to </summary>
    internal BlePeripheral Peripheral => _peripheral;

    /// <summary> Sends a notification to this central </summary>
    internal async ValueTask NotifyAsync(
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        if (!_notifications.TryEnter())
            throw DisconnectedError(innerException: null);
        try
        {
            CancellationToken lifetime = _lifetime.Token;
            using CancellationTokenSource? linked = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime)
                : null;
            CancellationToken token = linked?.Token ?? lifetime;
            // The next notification waits until the backend accepted this one; waiting for the turn stays cancellable
            await _notifyTurn.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await _driver.NotifyAsync(_connection, characteristic, value, token).ConfigureAwait(false);
            }
            finally
            {
                _notifyTurn.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (IsEnding)
        {
            // The connection ended meanwhile; the caller learns why, as from Disconnected
            throw DisconnectedError(e);
        }
        finally
        {
            _notifications.Exit();
        }
    }

    /// <summary> Gets the app's state of type <typeparamref name="T"/> for this connection, creating it on first use </summary>
    /// <remarks>
    /// The state is disposed once (<see cref="IAsyncDisposable"/> first, else <see cref="IDisposable"/>) after the connection
    /// ended and its running handlers returned, so a handler can use it until it returns. A factory that throws leaves no state
    /// </remarks>
    /// <param name="create"> Creates the state; runs at most once per type and connection </param>
    /// <typeparam name="T"> The type of the state; one per type </typeparam>
    /// <returns> The state </returns>
    /// <exception cref="ObjectDisposedException"> The connection ended and its states were disposed </exception>
    public T GetOrAddState<T>(Func<T> create)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(create);
        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_statesSealed, this);
            if (_states.TryGetValue(typeof(T), out object? existing))
                return (T)existing;
            T state = create();
            _states.Add(typeof(T), state);
            return state;
        }
    }

    /// <summary> The central changed its CCCD; ignored once the connection ends, so a late callback cannot bring it back </summary>
    internal void SetSubscribed(GattCharacteristicDefinition characteristic, bool enabled)
    {
        lock (_lock)
        {
            if (_ending is not null)
                return;
            if (enabled)
                _subscriptions.Add(characteristic);
            else
                _subscriptions.Remove(characteristic);
        }
    }

    internal bool IsSubscribed(GattCharacteristicDefinition characteristic)
    {
        lock (_lock)
            return _subscriptions.Contains(characteristic);
    }

    /// <summary> Runs a hosted handler for this central. An unexpected failure answers with <see cref="GattProtocolStatus.UnlikelyError"/> </summary>
    /// <remarks> Handlers of one central run one at a time; their token is cancelled when the connection ends </remarks>
    internal async Task<T> RunHandlerAsync<T>(
        string operation,
        BleUuid uuid,
        Func<CancellationToken, ValueTask<T>> handler
    )
    {
        if (!_handlers.TryEnter())
            throw Ended();
        try
        {
            CancellationToken lifetime = _lifetime.Token;
            try
            {
                await _handlerTurn.WaitAsync(lifetime).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                throw Ended();
            }
            // One span per handler run, after its turn; the handler's own spans nest inside
            using Activity? activity = Telemetry.Source.StartActivity("Handle {Operation} {Uuid}", ActivityKind.Server);
            if (activity is { IsAllDataRequested: true })
            {
                activity.SetTag(Telemetry.OperationTag, operation);
                activity.SetTag(Telemetry.UuidTag, Telemetry.Text(uuid));
                activity.SetTag(Telemetry.AddressTag, Telemetry.Text(Peer));
            }
            try
            {
                // Off the driver's thread: a handler may block before its first await
                T result = await Task.Run(async () => await handler(lifetime).ConfigureAwait(false), lifetime)
                    .ConfigureAwait(false);
                Telemetry.Finish(activity, Telemetry.Success);
                return result;
            }
            catch (GattException e) when (e.AttStatus is not null)
            {
                Telemetry.Finish(activity, Telemetry.AttError, e, _diagnostics);
                throw;
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                // The connection ends; there is nobody to answer
                Telemetry.Finish(activity, Telemetry.Cancelled);
                throw Ended();
            }
            catch (Exception e)
            {
                _logger.LogHandlerFailed(Peer, _diagnostics.Exception(e));
                Telemetry.Finish(activity, Telemetry.Error, e, _diagnostics);
                throw new GattException(GattProtocolStatus.UnlikelyError, "The handler failed", e);
            }
            finally
            {
                _handlerTurn.Release();
            }
        }
        finally
        {
            _handlers.Exit();
        }
    }

    /// <summary> Ends the connection exactly once; the first reason wins </summary>
    /// <param name="info"> Why it ended </param>
    /// <param name="takeLinkDown"> Whether the driver still has to take the link down; not when it reported the end </param>
    internal void End(DisconnectInfo info, bool takeLinkDown)
    {
        lock (_lock)
        {
            if (_ending is not null)
                return;
            _ending = info;
            _subscriptions.Clear();
        }
        // No handler or notification starts from now on
        Task handlersReturned = _handlers.CloseAndWaitAsync();
        _notifications.Close();
        lock (_lock)
        {
            _handlersReturned = handlersReturned;
            _slowHandlers = _timeProvider.CreateTimer(
                _ => WarnSlowHandlers(),
                state: null,
                SlowHandlerWarning,
                Timeout.InfiniteTimeSpan
            );
        }
        // Off the caller's thread: the driver reports a disconnect from its own loop
        _ = Task.Run(() => TeardownAsync(info, takeLinkDown));
    }

    private async Task TeardownAsync(DisconnectInfo info, bool takeLinkDown)
    {
        try
        {
            // Here, not in End: cancellation callbacks of the app must not run on the driver's thread
            await _lifetime.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // A failing callback of the app must not keep the link up
            _logger.LogConnectionCleanupFailed(Peer, _diagnostics.Exception(e));
        }
        try
        {
            if (takeLinkDown)
                await _driver.DisconnectAsync(_connection, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(Peer, _diagnostics.Exception(e));
        }
        finally
        {
            // A handler that ignores its token delays only this connection's end
            Task handlersReturned;
            lock (_lock)
                handlersReturned = _handlersReturned;
            await handlersReturned.ConfigureAwait(false);
            ITimer? slowHandlers;
            bool counted;
            lock (_lock)
            {
                slowHandlers = _slowHandlers;
                counted = _counted;
            }
            if (slowHandlers is not null)
                await slowHandlers.DisposeAsync().ConfigureAwait(false);
            if (counted)
                Telemetry.Connections.Add(-1, Tags());
            await _notifications.CloseAndWaitAsync().ConfigureAwait(false);
            await DisposeStatesAsync().ConfigureAwait(false);
            _lifetime.Dispose();
            _handlerTurn.Dispose();
            _notifyTurn.Dispose();
            _peripheral.Remove(_connection);
            _logger.LogCentralDisconnected(Peer, info.Reason);
            _disconnected.TrySetResult(info);
        }
    }

    /// <summary> The connection ends: the request gets no answer, as the link is going down (D9) </summary>
    private OperationCanceledException Ended() => new("The connection of the central ended", _lifetime.Token);

    private bool IsEnding
    {
        get
        {
            lock (_lock)
                return _ending is not null;
        }
    }

    private BleDisconnectedException DisconnectedError(Exception? innerException)
    {
        DisconnectInfo info;
        lock (_lock)
            info = _ending ?? throw new InvalidOperationException("The connection did not end");
        return new BleDisconnectedException(info, innerException);
    }

    /// <summary> Seals the states, then disposes each once; a failing disposal is logged and the others still run </summary>
    private async Task DisposeStatesAsync()
    {
        object[] states;
        lock (_stateLock)
        {
            _statesSealed = true;
            states = [.. _states.Values];
            _states.Clear();
        }
        foreach (object state in states)
        {
            try
            {
                if (state is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else
                    (state as IDisposable)?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogStateDisposeFailed(Peer, _diagnostics.Exception(e));
            }
        }
    }

    /// <summary> How long handlers may keep running after the connection started ending before a warning </summary>
    private static readonly TimeSpan SlowHandlerWarning = TimeSpan.FromSeconds(5);

    private void WarnSlowHandlers()
    {
        lock (_lock)
        {
            if (_handlersReturned.IsCompleted)
                return;
        }
        _logger.LogSlowHandlers(Peer, SlowHandlerWarning);
    }

    private TagList Tags() =>
        new() { { Telemetry.BackendTag, _peripheral.Adapter.Backend }, { Telemetry.RoleTag, "peripheral" } };
}
