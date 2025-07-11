using System.Diagnostics;
using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
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
public abstract class BleObserver(BleDevice device, ILogger<BleObserver> logger) : IAsyncDisposable, IBleObserver
{
    private readonly BleDevice _bleDevice = device;
    private readonly List<Action<IGapAdvertisement>> _actions = [];
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _observationStartSemaphore = new(1, 1);
    private ObserverState _observerState = ObserverState.Stopped;

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
        if (_observerState is not ObserverState.Stopped)
            return false;
        Parameters = parameters;
        return true;
    }

    /// <inheritdoc />
    public async Task StartObservingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));

        await _observationStartSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_observerState is ObserverState.Observing)
                return;

            Debug.Assert(_observerState == ObserverState.Stopped);
            _observerState = ObserverState.Starting;
            await StartObservingAsyncCore(cancellationToken).ConfigureAwait(false);
            _observerState = ObserverState.Observing;
            Logger.LogTrace("Started advertising observation");
        }
        catch (Exception e) when (e is not BleObservationStartException)
        {
            // Clean up the state
            _observerState = ObserverState.Stopped;
            throw new BleObservationStartException(this, e.Message, e);
        }
        finally
        {
            _observationStartSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public IDisposable OnAdvertisement<T>(T state, Action<T, IGapAdvertisement> onAdvertisement)
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));
        Action<IGapAdvertisement> action = advertisement => onAdvertisement(state, advertisement);
        lock (_lock)
        {
            _actions.Add(action);
        }

        return Disposable.Create(
            (this, action),
            static tuple =>
            {
                (BleObserver bleObserver, Action<IGapAdvertisement> action) = tuple;
                lock (bleObserver._lock)
                {
                    bleObserver._actions.Remove(action);
                }
            }
        );
    }

    /// <summary> Notify subscribers of a new advertisement </summary>
    /// <param name="advertisement"> The advertisement </param>
    protected void OnNext(IGapAdvertisement advertisement)
    {
        lock (_lock)
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                try
                {
                    var onAdvertisement = _actions[i];
                    onAdvertisement(advertisement);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, "Exception while handling advertisement event: {Message}", e.Message);
                }
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
        await _observationStartSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Return early if
            // Stopped -> Nothing to do
            // Stopping -> Some recursive call has lead to us being here
            if (_observerState is ObserverState.Stopping or ObserverState.Stopped)
                return;

            Debug.Assert(_observerState == ObserverState.Observing);
            _observerState = ObserverState.Stopping;
            await StopObservingAsyncCore().ConfigureAwait(false);
            Logger.LogTrace("Stopped advertising observation");
            _observerState = ObserverState.Stopped;
        }
        finally
        {
            _observationStartSemaphore.Release();
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
        lock (_lock)
        {
            _actions.Clear();
        }
        _observationStartSemaphore.Dispose();
        await DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
