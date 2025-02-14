namespace Darp.Ble.Hci.Reactive;

/// <summary> The implementation of the <see cref="RefObservable.First{T}"/> operator </summary>
/// <param name="outerObserver"> The outer observer </param>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
file sealed class FirstRefObserver<T>(IRefObserver<T> outerObserver) : IRefObserver<T>
    where T : allows ref struct
{
    private readonly IRefObserver<T> _outerObserver = outerObserver;

    /// <inheritdoc />
    public void OnNext(T value)
    {
        _outerObserver.OnNext(value);
        _outerObserver.OnCompleted();
    }

    /// <inheritdoc />
    public void OnError(Exception error) => _outerObserver.OnError(error);

    /// <inheritdoc />
    public void OnCompleted() => _outerObserver.OnCompleted();
}

public static partial class RefObservable
{
    /// <summary> Returns the first element of an observable sequence. </summary>
    /// <param name="source"> A sequence of elements to invoke a transform function on. </param>
    /// <typeparam name="T"> The type of the elements in the source sequence. </typeparam>
    /// <returns> An observable sequence that contains elements from the input sequence that satisfy the condition. </returns>
    public static IRefObservable<T> First<T>(this IRefObservable<T> source)
        where T : allows ref struct
    {
        return Create<T, IRefObservable<T>>(
            source,
            (state, observer) => state.Subscribe(new FirstRefObserver<T>(observer))
        );
    }
}
