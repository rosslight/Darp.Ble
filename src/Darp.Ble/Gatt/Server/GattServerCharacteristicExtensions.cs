using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Darp.Ble.Gatt.Server;

/// <summary> An observable which provides a dispose method to allow unsubscription from something else </summary>
/// <typeparam name="T"></typeparam>
public interface IDisposableObservable<out T> : IObservable<T>, IDisposable;

/// <summary> An implementation of <see cref="IDisposableObservable{T}"/> </summary>
/// <param name="source"> The source for the observable </param>
/// <param name="disposable"> The disposable </param>
/// <typeparam name="T"> The type of the observable </typeparam>
public sealed class DisposableObservable<T>(IObservable<T> source, IDisposable disposable) : IDisposableObservable<T>
{
    private readonly IObservable<T> _source = source;
    private readonly IDisposable _disposable = disposable;

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<T> observer) => _source.Subscribe(observer);
    /// <inheritdoc />
    public void Dispose() => _disposable.Dispose();
}

/// <summary> Extensions for <see cref="IGattServerCharacteristic"/> </summary>
public static class GattServerCharacteristicExtensions
{
    /// <inheritdoc cref="IGattServerCharacteristic.OnNotify"/>
    /// <param name="characteristic">The characteristic with notify property</param>
    public static Task<IDisposable> OnNotifyAsync(this IGattServerCharacteristic<Properties.Notify> characteristic,
        Action<byte[]> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.OnNotifyAsync(callback, (action, memory) => action(memory), cancellationToken);
    }

    /// <summary>
    /// Enable notifications and get a <see cref="IDisposableObservable{T}"/> which allows unsubscription as well as listening to the events.
    /// Just unsubscribing from the observable does not unsubscribe completely.
    /// </summary>
    /// <param name="characteristic">The characteristic with notify property</param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial subscription process </param>
    /// <returns> A task which completes when notifications are enabled. Returns a disposable observable </returns>
    public static async Task<IDisposableObservable<byte[]>> OnNotifyAsync(this IGattServerCharacteristic<Properties.Notify> characteristic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
#pragma warning disable CA2000
        var subject = new Subject<byte[]>();
        IDisposable disposable = await characteristic.Characteristic
            .OnNotifyAsync(subject, (s, bytes) => s.OnNext(bytes), cancellationToken)
            .ConfigureAwait(false);
        IDisposable combinedDisposable = Disposable.Create(() =>
        {
            disposable.Dispose();
            subject.OnCompleted();
            subject.Dispose();
        });
#pragma warning restore CA2000
        return new DisposableObservable<byte[]>(subject, combinedDisposable);
    }

    /// <inheritdoc cref="IGattServerCharacteristic.WriteAsync"/>
    /// <param name="characteristic">The characteristic with notify property</param>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    public static async Task WriteAsync(this IGattServerCharacteristic<Properties.Write> characteristic,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        await characteristic.Characteristic.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}