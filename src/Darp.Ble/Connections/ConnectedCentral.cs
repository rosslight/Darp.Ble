using Darp.Ble.Backends;
using Darp.Ble.Data;
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
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private DisconnectInfo? _ending;

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
        ILogger logger
    )
    {
        _peripheral = peripheral;
        _driver = driver;
        _connection = connection;
        _logger = logger;
        Address = connection.Address;
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

    /// <summary> Runs a hosted handler for this central. An unexpected failure answers with <see cref="GattProtocolStatus.UnlikelyError"/> </summary>
    /// <remarks> Handlers of one central run one at a time; their token is cancelled when the connection ends </remarks>
    internal async Task<T> RunHandlerAsync<T>(Func<CancellationToken, ValueTask<T>> handler)
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
            try
            {
                // Off the driver's thread: a handler may block before its first await
                return await Task.Run(async () => await handler(lifetime).ConfigureAwait(false), lifetime)
                    .ConfigureAwait(false);
            }
            catch (GattException e) when (e.AttStatus is not null)
            {
                throw;
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                // The connection ends; there is nobody to answer
                throw Ended();
            }
            catch (Exception e)
            {
                _logger.LogHandlerFailed(Address, e);
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
        }
        // No handler or notification starts from now on
        _handlers.Close();
        _notifications.Close();
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
            _logger.LogConnectionCleanupFailed(Address, e);
        }
        try
        {
            if (takeLinkDown)
                await _driver.DisconnectAsync(_connection, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(Address, e);
        }
        finally
        {
            // A handler that ignores its token delays only this connection's end
            await _handlers.CloseAndWaitAsync().ConfigureAwait(false);
            await _notifications.CloseAndWaitAsync().ConfigureAwait(false);
            _lifetime.Dispose();
            _handlerTurn.Dispose();
            _notifyTurn.Dispose();
            _peripheral.Remove(_connection);
            _logger.LogCentralDisconnected(Address, info.Reason);
            _disconnected.TrySetResult(info);
        }
    }

    private static GattException Ended() =>
        new(GattProtocolStatus.UnlikelyError, "The connection of the central ended");

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
}
