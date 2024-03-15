using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Darp.Ble.Implementation;

namespace Darp.Ble;

public sealed class BleObserver : IConnectableObservable<IGapAdvertisement>
{
    private readonly IBleObserverImplementation _bleDeviceObserver;
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    private IObservable<IGapAdvertisement>? _scanObservable;
    private IDisposable? _scanDisposable;

    public BleObserver(IBleObserverImplementation bleDeviceObserver)
    {
        _bleDeviceObserver = bleDeviceObserver;
    }

    public bool IsScanning => _scanDisposable is not null;

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

    public IDisposable Connect()
    {
        lock (this)
        {
            if (_scanDisposable is not null) return _scanDisposable;
            var result = _bleDeviceObserver.TryStartScan(out var observable);
            if (!result || observable is null)
            {
                return Disposable.Empty;
            }

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

public interface IGapAdvertisement
{
}