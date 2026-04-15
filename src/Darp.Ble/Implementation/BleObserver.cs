using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;
#if !NET9_0_OR_GREATER
using Lock = System.Object;
#endif

namespace Darp.Ble.Implementation;

/// <summary> The observer state </summary>
internal enum ObserverState
{
    Stopped,
    Starting,
    Observing,
    Stopping,
}

/// <summary> The ble observer </summary>
/// <param name="device"> The ble device </param>
/// <param name="logger"> The logger </param>
public abstract class BleObserver(BleDevice device, ILogger<BleObserver> logger) : IBleObserver, IAsyncDisposable
{
    private readonly record struct AdvertisementHandlerSubscription(
        Action<IGapAdvertisement> OnAdvertisement,
        Action<Exception>? OnError
    );

    private readonly BleDevice _bleDevice = device;
    private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);
    private readonly Lock _handlersLock = new();
    private AdvertisementHandlerSubscription[] _handlers = [];

    private volatile ObserverState _observerState = ObserverState.Stopped;

    /// <summary> The logger </summary>
    protected ILogger<BleObserver> Logger { get; } = logger;

    /// <summary> The service provider </summary>
    protected IServiceProvider ServiceProvider => Device.ServiceProvider;

    /// <inheritdoc />
    public IBleDevice Device => _bleDevice;

    /// <inheritdoc />
    public BleObservationParameters Parameters { get; private set; } =
        new()
        {
            ScanType = ScanType.Passive,
            ScanInterval = ScanTiming.Ms100,
            ScanWindow = ScanTiming.Ms100,
        };

    /// <inheritdoc />
    public bool IsObserving => _observerState is ObserverState.Observing;

    /// <inheritdoc />
    public bool Configure(BleObservationParameters parameters)
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));

        if (!_startStopSemaphore.Wait(0))
            return false;
        try
        {
            if (_observerState is not ObserverState.Stopped)
                return false;
            Parameters = parameters;
            return true;
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StartObservingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));

        await _startStopSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_observerState is ObserverState.Observing)
                return;
            if (_observerState is not ObserverState.Stopped)
                throw new InvalidOperationException($"Observer is in invalid state {_observerState}");

            _observerState = ObserverState.Starting;
            await StartObservingAsyncCore(cancellationToken).ConfigureAwait(false);
            _observerState = ObserverState.Observing;
            Logger.LogObserverStarted();
        }
        catch (Exception e) when (e is not BleObservationStartException)
        {
            // Clean up the state
            _observerState = ObserverState.Stopped;
            throw new BleObservationStartException(this, e.Message, e);
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public IDisposable OnAdvertisement(Action<IGapAdvertisement> onAdvertisement, Action<Exception>? onError = null)
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));

        var subscription = new AdvertisementHandlerSubscription(onAdvertisement, onError);

        // Extend handlers list
        lock (_handlersLock)
        {
            AdvertisementHandlerSubscription[] oldHandlers = _handlers;
            var newArr = new AdvertisementHandlerSubscription[oldHandlers.Length + 1];
            Array.Copy(oldHandlers, newArr, oldHandlers.Length);
            newArr[^1] = subscription;
            Volatile.Write(ref _handlers, newArr);
        }

        return Disposable.Create(
            (this, subscription),
            static tuple =>
            {
                (BleObserver self, AdvertisementHandlerSubscription handler) = tuple;
                lock (self._handlersLock)
                {
                    if (Helpers.TryRemoveImmutable(self._handlers, handler, out var newHandlers))
                        Volatile.Write(ref self._handlers, newHandlers);
                }
            }
        );
    }

    /// <summary> Notify subscribers of a new advertisement </summary>
    /// <param name="advertisement"> The advertisement </param>
    protected void OnNext(IGapAdvertisement advertisement)
    {
        // Try to suppress receival of advertisements after stopping/disposal
        // Best-effort only. No thread safety guarantees
        if (_bleDevice.IsDisposing || _observerState is ObserverState.Stopping or ObserverState.Stopped)
            return;

        // Taking the current snapshot of the handlers.
        // In case of an unsubscription of a handler we might have taken the reference here already and call it afterward.
        // This is a known tradeoff
        AdvertisementHandlerSubscription[] handlers = Volatile.Read(ref _handlers);
        foreach (AdvertisementHandlerSubscription handler in handlers)
        {
            try
            {
                handler.OnAdvertisement(advertisement);
            }
            catch (Exception e)
            {
                // An exception inside the handler should not crash all observers. Logging and ignoring ...
                Logger.LogObservationErrorDuringAdvertisementHandling(e);
            }
        }
    }

    /// <summary> Notify subscribers of a fatal observation error. Existing subscriptions are terminated. </summary>
    /// <param name="exception"> The exception that triggered the error </param>
    protected async Task OnErrorAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_bleDevice.IsDisposing)
            return;

        AdvertisementHandlerSubscription[] handlers;
        await _startStopSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            handlers = Volatile.Read(ref _handlers);
            if (handlers.Length == 0 && _observerState is ObserverState.Stopped)
                return;

            _observerState = ObserverState.Stopped;
            lock (_handlersLock)
            {
                _handlers = [];
            }
        }
        finally
        {
            _startStopSemaphore.Release();
        }

        Logger.LogObservationFailed(exception);

        foreach (AdvertisementHandlerSubscription handler in handlers)
        {
            if (handler.OnError is null)
                continue;

            try
            {
                handler.OnError(exception);
            }
            catch (Exception e)
            {
                Logger.LogObservationErrorDuringErrorHandling(e);
            }
        }
    }

    /// <summary> Core implementation to start observing async </summary>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A task that completes when observation has started </returns>
    protected abstract Task StartObservingAsyncCore(CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task StopObservingAsync()
    {
        await _startStopSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Return early if
            // Stopped -> Nothing to do
            // Stopping -> Some recursive call has lead to us being here
            if (_observerState is ObserverState.Stopping or ObserverState.Stopped)
                return;
            if (_observerState is not ObserverState.Observing)
                throw new InvalidOperationException($"Observer is in invalid state {_observerState}");

            _observerState = ObserverState.Stopping;

            try
            {
                await StopObservingAsyncCore().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogObserverErrorDuringStopping(e);
                // In case of an error when stopping we assume we are still observing
                // Not ideal, but better than to wait for ever
                _observerState = ObserverState.Observing;
                throw;
            }
            Logger.LogObserverStopped();
            _observerState = ObserverState.Stopped;
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    /// <summary> Core implementation of stopping </summary>
    protected abstract Task StopObservingAsyncCore();

    /// <summary> A method that can be used to clean up all resources. </summary>
    /// <remarks> This method is not glued to the <see cref="IAsyncDisposable"/> interface. All disposes should be done using the  </remarks>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopObservingAsync().ConfigureAwait(false);
        lock (_handlersLock)
        {
            _handlers = [];
        }
        _startStopSemaphore.Dispose();
        await DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
