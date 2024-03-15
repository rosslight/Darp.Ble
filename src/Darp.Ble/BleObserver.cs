using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble;

/// <summary> The ble observer </summary>
public sealed class BleObserver : IConnectableObservable<IGapAdvertisement>
{
    private readonly IBleObserverImplementation _bleDeviceObserver;
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    private IObservable<IGapAdvertisement>? _scanObservable;
    private IDisposable? _scanDisposable;

    internal BleObserver(IBleObserverImplementation bleDeviceObserver)
    {
        _bleDeviceObserver = bleDeviceObserver;
    }

    /// <summary> True if the observer is currently scanning </summary>
    public bool IsScanning => _scanDisposable is not null;

    /// <summary> Stop the scan that is currently running </summary>
    public void StopScan()
    {
        lock (this)
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
        lock (this)
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
    /// <returns>Disposable used to disconnect the observable wrapper from its source, causing subscribed observer to stop receiving values from the underlying observable sequence.</returns>
    public IDisposable Connect()
    {
        lock (this)
        {
            if (_scanDisposable is not null) return _scanDisposable;
            var result = _bleDeviceObserver.TryStartScan(out var observable);
            if (!result) return Disposable.Empty;

            _scanDisposable = Disposable.Create(this, state =>
            {
                state.StopScan();
            });
            _scanObservable = observable;
            // Make copy of observers to be resilient to disconnections on first connection
            foreach (var observer in _observers.ToArray())
            {
                observable.Subscribe(observer);
            }
            return _scanDisposable;
        }
    }
}