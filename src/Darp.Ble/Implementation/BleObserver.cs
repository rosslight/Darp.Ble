using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Logger;

namespace Darp.Ble.Implementation;

/// <summary> The ble observer </summary>
/// <param name="device"> The ble device </param>
/// <param name="logger"> The logger </param>
public abstract class BleObserver(BleDevice device, IObserver<LogEvent>? logger) : IBleObserver
{
    /// <summary> The logger </summary>
    protected IObserver<LogEvent>? Logger { get; } = logger;
    private readonly object _lockObject = new();
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    private bool _isDisposed;
    private bool _stopping;
    private IObservable<IGapAdvertisement>? _scanObservable;
    private IDisposable? _scanDisposable;

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;
    /// <inheritdoc />
    public BleScanParameters Parameters { get; private set; } = new()
    {
        ScanType = ScanType.Passive,
        ScanInterval = ScanTiming.Ms100,
        ScanWindow = ScanTiming.Ms100,
    };
    /// <inheritdoc />
    public bool IsScanning => _scanDisposable is not null;

    /// <inheritdoc />
    public bool Configure(BleScanParameters parameters)
    {
        if (IsScanning) return false;
        Parameters = parameters;
        return true;
    }

    /// <summary>
    /// Subscribe to the ble observer. Will not start the observation until <see cref="Connect"/> was called.
    /// </summary>
    /// <param name="observer">The object that is to receive notifications.</param>
    /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
    /// <exception cref="ObjectDisposedException"> Thrown if the <see cref="BleObserver"/> was disposed </exception>
    public IDisposable Subscribe(IObserver<IGapAdvertisement> observer)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BleObserver));
        lock (_lockObject)
        {
            IDisposable? optDisposable = _scanObservable?.Subscribe(observer);
            _observers.Add(observer);
            return Disposable.Create((This: this, Observer: observer, Disposable: optDisposable), state =>
            {
                state.Disposable?.Dispose();
                state.This._observers.Remove(state.Observer);
                if (state.This._observers.Count == 0)
                    state.This.StopScan();
            });
        }
    }

    /// <summary>
    /// Start a new connection. All observers will receive advertisement events.
    /// If called while an observation is running nothing happens and the disposable to cancel the scan is returned
    /// </summary>
    /// <returns> Disposable used to stop the advertisement scan. Subscribed observables will be completed. </returns>
    /// <exception cref="ObjectDisposedException"> Thrown if the <see cref="BleObserver"/> was disposed </exception>
    public IDisposable Connect()
    {
        if(_isDisposed)
            return Disposable.Empty;
        lock (_lockObject)
        {
            if (_scanDisposable is not null) return _scanDisposable;
            bool startScanSuccessful = TryStartScanCore(out IObservable<IGapAdvertisement> observable);

            observable = observable
                .Catch((Exception exception) => Observable.Throw<IGapAdvertisement>(exception switch
                {
                    BleObservationException e => e,
                    _ => new BleObservationException(this, message: null, exception),
                }));
            // Use for loop to be resilient to disconnections on first connection
            for (int index = _observers.Count - 1; index >= 0; index--)
            {
                observable.Subscribe(_observers[index]);
            }

            if (!startScanSuccessful) return Disposable.Empty;

            _scanObservable = observable;
            _scanDisposable = Disposable.Create(this, self => self.StopScan());
            return _scanDisposable;
        }
    }

    /// <summary> Core implementation of scan start </summary>
    /// <param name="observable"> The resulting observable. Should be failing if there was an error </param>
    /// <returns> True, if the start was successful </returns>
    protected abstract bool TryStartScanCore(out IObservable<IGapAdvertisement> observable);

    void IBleObserver.StopScan() => StopScan(reason: null);

    /// <inheritdoc cref="IBleObserver.StopScan"/>
    /// <param name="reason">
    /// Supply optional reason for stoppage. Supplying the reason will cause subscribers to complete with an error
    /// </param>
    public void StopScan(Exception? reason = null)
    {
        lock (_lockObject)
        {
            if (_stopping) return;
            _stopping = true;
            try
            {
                for (int index = _observers.Count - 1; index >= 0; index--)
                {
                    IObserver<IGapAdvertisement> obs = _observers[index];
                    if (reason is not null)
                        obs.OnError(reason);
                    else
                        obs.OnCompleted();
                }
                _observers.Clear();
                StopScanCore();
                _scanDisposable?.Dispose();
                _scanDisposable = null;
            }
            finally
            {
                _stopping = false;
            }
        }
    }

    /// <summary> Core implementation of stopping </summary>
    protected abstract void StopScanCore();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if(_isDisposed) return;
        _isDisposed = true;
        DisposeCore();
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}