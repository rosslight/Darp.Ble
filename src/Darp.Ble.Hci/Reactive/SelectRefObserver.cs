namespace Darp.Ble.Hci.Reactive;

/// <summary> The implementation of the <see cref="RefObservable.Select{T,TOut}"/> operator </summary>
/// <param name="outerObserver"> The outer observer </param>
/// <param name="selector"> The selector to transform the data </param>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
/// <typeparam name="TOut"> The object that provides notification information after the selector was applied. </typeparam>
file sealed class SelectRefObserver<T, TOut>(IRefObserver<TOut> outerObserver, Func<T, TOut> selector) : IRefObserver<T>
    where T : allows ref struct
    where TOut : allows ref struct
{
    private readonly IRefObserver<TOut> _outerObserver = outerObserver;
    private readonly Func<T, TOut> _selector = selector;

    /// <inheritdoc />
    public void OnNext(T value) => _outerObserver.OnNext(_selector(value));

    /// <inheritdoc />
    public void OnError(Exception error) => _outerObserver.OnError(error);

    /// <inheritdoc />
    public void OnCompleted() => _outerObserver.OnCompleted();
}

public static partial class RefObservable
{
    /// <summary> Projects each element of an observable sequence into a new form by incorporating the element's index. </summary>
    /// <param name="source"> A sequence of elements to invoke a transform function on. </param>
    /// <param name="selector"> A transform function to apply to each source element. </param>
    /// <typeparam name="T"> The type of the elements in the source sequence. </typeparam>
    /// <typeparam name="TOut"> The type of the elements in the resulting sequence </typeparam>
    /// <returns> An observable sequence whose elements are the result of invoking the transform function on each element of source. </returns>
    public static IRefObservable<TOut> Select<T, TOut>(this IRefObservable<T> source, Func<T, TOut> selector)
        where T : allows ref struct
        where TOut : allows ref struct
    {
        return Create<TOut, (IRefObservable<T> Source, Func<T, TOut> Selector)>(
            (source, selector),
            (state, observer) => state.Source.Subscribe(new SelectRefObserver<T, TOut>(observer, state.Selector))
        );
    }
}
