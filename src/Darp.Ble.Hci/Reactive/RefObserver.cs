namespace Darp.Ble.Hci.Reactive;

file sealed class RefObserver<T>(Action<T>? onValue, Action<Exception>? onError, Action? onCompleted) : IRefObserver<T>
    where T : allows ref struct
{
    private readonly Action<T>? _onValue = onValue;
    private readonly Action<Exception>? _onError = onError;
    private readonly Action? _onCompleted = onCompleted;

    public void OnNext(T value) => _onValue?.Invoke(value);

    public void OnError(Exception error) => _onError?.Invoke(error);

    public void OnCompleted() => _onCompleted?.Invoke();
}

public static partial class RefObservable
{
    /// <summary> Subscribe to the given ref observable </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
    /// <param name="onError">Action to invoke upon exceptional termination of the observable sequence.</param>
    /// <param name="onCompleted">Action to invoke upon graceful termination of the observable sequence.</param>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    public static IDisposable Subscribe<T>(
        this IRefObservable<T> source,
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null
    )
        where T : allows ref struct
    {
        ArgumentNullException.ThrowIfNull(source);
        var observer = new RefObserver<T>(onNext, onError, onCompleted);
        return source.Subscribe(observer);
    }
}
