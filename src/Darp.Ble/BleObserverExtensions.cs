using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Gap;

namespace Darp.Ble;

/// <summary> Extensions for <see cref="IBleObserver"/> </summary>
public static class BleObserverExtensions
{
    /// <summary>
    /// Observes advertisements broadcast by BLE devices using the provided <see cref="IBleObserver"/>.
    /// </summary>
    /// <param name="observer">The instance of <see cref="IBleObserver"/> that will monitor BLE advertisements.</param>
    /// <returns>An observable sequence of <see cref="IGapAdvertisement"/> instances representing BLE advertisements.</returns>
    public static IObservable<IGapAdvertisement> OnAdvertisement(this IBleObserver observer)
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

    /// <summary> Publish the observer to allow observing advertisements without having to start/stop observation manually </summary>
    /// <param name="bleObserver"> The observer to publish </param>
    /// <returns> A connectable observable which supports scanning on subscription </returns>
    public static IConnectableObservable<IGapAdvertisement> Publish(this IBleObserver bleObserver)
    {
        ArgumentNullException.ThrowIfNull(bleObserver);

        IObservable<IGapAdvertisement> inner = Observable.Create<IGapAdvertisement>(observer =>
        {
            IDisposable unhook = bleObserver.OnAdvertisement(
                observer,
                onAdvertisement: static (observer, adv) => observer.OnNext(adv)
            );

            bleObserver.StartObservingAsync().FireAndForget(observer.OnError);

            return Disposable.Create(() =>
            {
                unhook.Dispose();
                // If task fails, there is nothing we can do - the observer already unsubscribed
                bleObserver.StopObservingAsync().FireAndForget(_ => { });
            });
        });

        return inner.Publish();
    }

    private static async void FireAndForget(this Task task, Action<Exception> onError)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            onError(e);
        }
    }
}
