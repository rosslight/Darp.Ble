using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary>
/// 
/// </summary>
public record BleObserverConfiguration
{
    
}

/// <summary> The ble observer </summary>
public sealed class BleObserver : IConnectableObservable<IGapAdvertisement>
{
    private readonly IBleObserverImplementation _bleDeviceObserver;
    private readonly IObserver<LogEvent>? _logger;
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    private IObservable<IGapAdvertisement>? _scanObservable;
    private IDisposable? _scanDisposable;
    private readonly object _lockObject = new();

    internal BleObserver(IBleObserverImplementation bleDeviceObserver, IObserver<LogEvent>? logger)
    {
        _bleDeviceObserver = bleDeviceObserver;
        _logger = logger;
    }

    public void Configure(BleObserverConfiguration configuration)
    {
        
    }

    /// <summary> True if the observer is currently scanning </summary>
    public bool IsScanning => _scanDisposable is not null;

    /// <summary> Stop the scan that is currently running </summary>
    public void StopScan()
    {
        lock (_lockObject)
        {
            _bleDeviceObserver.StopScan();
            _scanDisposable?.Dispose();
            _scanDisposable = null;
            _scanObservable = null;
        }
    }

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

    private void ThrowAll(Exception exception)
    {
        lock (_lockObject)
        {
            foreach (IObserver<IGapAdvertisement> observer in _observers.ToArray()) observer.OnError(exception);
            _observers.Clear();
        }
    }

    /// <summary>
    /// Start a new connection. All observers will receive advertisement events.
    /// If called while an observation is running nothing happens and the disposable to cancel the scan is returned
    /// </summary>
    /// <returns>Disposable used to disconnect the observable wrapper from its source, causing subscribed observer to stop receiving values from the underlying observable sequence.</returns>
    public IDisposable Connect()
    {
        lock (_lockObject)
        {
            if (_scanDisposable is not null) return _scanDisposable;
            bool startScanSuccessful = _bleDeviceObserver.TryStartScan(this, out IObservable<IGapAdvertisement> observable);

            observable = observable
                .Catch((Exception exception) => Observable.Throw<IGapAdvertisement>(exception switch
                {
                    BleObservationStartUnsuccessfulException e => e,
                    _ => new BleObservationStartUnsuccessfulException(this, exception)
                }));
            // Make copy of observers (.ToArray()) to be resilient to disconnections on first connection
            foreach (IObserver<IGapAdvertisement> observer in _observers.ToArray()) observable.Subscribe(observer);

            if (!startScanSuccessful)
            {
                return Disposable.Empty;
            }

            _scanObservable = observable;
            _scanDisposable = Disposable.Create(this, state => state.StopScan());
            return _scanDisposable;
        }
    }
}