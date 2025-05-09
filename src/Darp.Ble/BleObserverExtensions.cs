using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble;

/// <summary> Extensions for <see cref="IBleObserver"/> </summary>
public static class BleObserverExtensions
{
    /// <summary>
    /// Observes advertisements broadcast by BLE devices using the provided <see cref="IBleObserver"/>.
    /// </summary>
    /// <param name="observer">The instance of <see cref="IBleObserver"/> that will monitor BLE advertisements.</param>
    /// <returns>An observable sequence of <see cref="IGapAdvertisement"/> instances representing BLE advertisements.</returns>
    public static IObservable<IGapAdvertisement> Observe(this IBleObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Observable.Create<IGapAdvertisement>(advObserver =>
            observer.OnAdvertisement(
                advObserver,
                static (advObserver, advertisement) => advObserver.OnNext(advertisement)
            )
        );
    }

    /// <summary> Register a callback called when an advertisement was received </summary>
    /// <param name="bleObserver">The instance of <see cref="IBleObserver"/> that will monitor BLE advertisements.</param>
    /// <param name="onAdvertisement"> The callback </param>
    /// <returns> A disposable to unsubscribe the callback </returns>
    public static IDisposable OnAdvertisement(this IBleObserver bleObserver, Action<IGapAdvertisement> onAdvertisement)
    {
        ArgumentNullException.ThrowIfNull(bleObserver);
        return bleObserver.OnAdvertisement(onAdvertisement, static (action, advertisement) => action(advertisement));
    }
}

file sealed class AdvertisementObservable : IObservable<IGapAdvertisement>
{
    private readonly object _lock = new();
    private readonly List<IObserver<IGapAdvertisement>> _observers = [];
    internal IAsyncDisposable? ScanDisposable { get; set; }

    public void OnNext(IGapAdvertisement advertisement)
    {
        lock (_lock)
        {
            for (int index = _observers.Count - 1; index >= 0; index--)
            {
                IObserver<IGapAdvertisement> observer = _observers[index];
                observer.OnNext(advertisement);
            }
        }
    }

    public void OnCompleted()
    {
        lock (_lock)
        {
            for (int index = _observers.Count - 1; index >= 0; index--)
            {
                IObserver<IGapAdvertisement> observer = _observers[index];
                observer.OnCompleted();
            }

            _observers.Clear();
        }
    }

    public IDisposable Subscribe(IObserver<IGapAdvertisement> observer)
    {
        lock (_lock)
        {
            _observers.Add(observer);
        }
        return Disposable.Create(
            (this, observer),
            state =>
            {
                lock (_lock)
                {
                    state.Item1._observers.Remove(state.observer);
                    if (ScanDisposable is null)
                        return;
                    ValueTask task = ScanDisposable.DisposeAsync();
                    if (!task.IsCompletedSuccessfully)
                    {
                        // Fire valueTask if not completed
                        _ = task.AsTask();
                    }
                    ScanDisposable = null;
                }
            }
        );
    }
}
