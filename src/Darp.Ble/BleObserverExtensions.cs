using System.Reactive.Subjects;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble;

/// <summary> Extensions for <see cref="IBleObserver"/> </summary>
public static class BleObserverExtensions
{
    /// <summary>
    /// Enable notifications and get a <see cref="IDisposableObservable{T}"/> which allows unsubscription as well as listening to the events.
    /// Unsubscribing from the returned disposable does only stop advertising if there are no additional observers listening
    /// </summary>
    /// <param name="observer">The characteristic with notify property</param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial subscription process </param>
    /// <returns> A task which completes when notifications are enabled. Returns a disposable observable </returns>
    public static async Task<IDisposableObservable<IGapAdvertisement>> StartObservingAsync(
        this IBleObserver observer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(observer);
#pragma warning disable CA2000
        var subject = new Subject<IGapAdvertisement>();
        IAsyncDisposable disposable = await observer
            .StartObservingAsync(adv => subject.OnNext(adv), () => subject.OnCompleted(), cancellationToken)
            .ConfigureAwait(false);
        IAsyncDisposable combinedDisposable = AsyncDisposable.Create(async () =>
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
            subject.OnCompleted();
            subject.Dispose();
        });
#pragma warning restore CA2000
        return new DisposableObservable<IGapAdvertisement>(subject, combinedDisposable);
    }
}
