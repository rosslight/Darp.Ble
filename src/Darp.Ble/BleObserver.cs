using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Logger;

namespace Darp.Ble;

public interface IBleObserver : IConnectableObservable<IGapAdvertisement>, IAsyncDisposable
{
    
}

/// <summary> The ble observer </summary>
public abstract class BleObserver(BleDevice device, IObserver<LogEvent>? logger) : IBleObserver
{
    private readonly IObserver<LogEvent>? _logger = logger;
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    private IObservable<IGapAdvertisement>? _scanObservable;
    private IDisposable? _scanDisposable;
    private readonly object _lockObject = new();

    /// <summary> The ble device </summary>
    public BleDevice Device { get; } = device;
    /// <summary> The parameters used for the current scan </summary>
    public BleScanParameters Parameters { get; private set; } = new()
    {
        ScanType = ScanType.Passive,
        ScanInterval = ScanTiming.Ms100,
        ScanWindow = ScanTiming.Ms100,
    };

    /// <summary> True if the observer is currently scanning </summary>
    public bool IsScanning => _scanDisposable is not null;

    /// <summary>
    /// Set a new configuration for advertising observation. Setting is only allowed while observer is not scanning
    /// </summary>
    /// <param name="parameters"> The configuration to set </param>
    /// <returns> True, if setting parameters was successful </returns>
    public bool Configure(BleScanParameters parameters)
    {
        if (IsScanning) return false;
        Parameters = parameters;
        return true;
    }

    /// <summary> Stop the scan that is currently running </summary>
    /// <param name="reason">
    /// Supply optional reason for stoppage. Supplying the reason will cause subscribers to complete with an error
    /// </param>
    public void StopScan(Exception? reason = null)
    {
        lock (_lockObject)
        {
            if (reason is not null)
            {
                foreach (IObserver<IGapAdvertisement> observer in _observers.ToArray()) observer.OnError(reason);
                _observers.Clear();
            }
            StopScanCore();
            _scanDisposable?.Dispose();
            _scanDisposable = null;
            _scanObservable = null;
        }
    }

    protected abstract void StopScanCore();

    /// <summary>
    /// Subscribe to the ble observer. Will not start the observation until <see cref="Connect"/> was called.
    /// </summary>
    /// <param name="observer">The object that is to receive notifications.</param>
    /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
    public IDisposable Subscribe(IObserver<IGapAdvertisement> observer)
    {
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
    public IDisposable Connect()
    {
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
            _scanDisposable = Disposable.Create(this, state => state.StopScan());
            return _scanDisposable;
        }
    }

    protected abstract bool TryStartScanCore(out IObservable<IGapAdvertisement> observable);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeSyncInternal();
        await DisposeInternalAsync();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeInternalAsync() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeSyncInternal() { }
}