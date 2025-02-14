namespace Darp.Ble.Hci.Reactive;

/// <summary> The implementation of the <see cref="RefObservable.Where{T}"/> operator </summary>
/// <param name="outerObserver"> The outer observer </param>
/// <param name="predicate"> A function to test each source element for a condition. </param>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
file sealed class WhereRefObserver<T>(IRefObserver<T> outerObserver, Func<T, bool> predicate) : IRefObserver<T>
    where T : allows ref struct
{
    private readonly IRefObserver<T> _outerObserver = outerObserver;
    private readonly Func<T, bool> _predicate = predicate;

    /// <inheritdoc />
    public void OnNext(T value)
    {
        if (_predicate(value))
            _outerObserver.OnNext(value);
    }

    /// <inheritdoc />
    public void OnError(Exception error) => _outerObserver.OnError(error);

    /// <inheritdoc />
    public void OnCompleted() => _outerObserver.OnCompleted();
}

public static partial class RefObservable
{
    /// <summary> Filters the elements of an observable sequence based on a predicate. </summary>
    /// <param name="source"> A sequence of elements to invoke a transform function on. </param>
    /// <param name="predicate"> A function to test each source element for a condition. </param>
    /// <typeparam name="T"> The type of the elements in the source sequence. </typeparam>
    /// <returns> An observable sequence that contains elements from the input sequence that satisfy the condition. </returns>
    public static IRefObservable<T> Where<T>(this IRefObservable<T> source, Func<T, bool> predicate)
        where T : allows ref struct
    {
        return Create<T, (IRefObservable<T> Source, Func<T, bool> Predicate)>(
            (source, predicate),
            (state, observer) => state.Source.Subscribe(new WhereRefObserver<T>(observer, state.Predicate))
        );
    }
}
