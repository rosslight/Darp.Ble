namespace Darp.Ble.Hci.Reactive;

/// <summary> The implementation of the <see cref="RefObservable.TakeUntil{T,TOther}(Darp.Ble.Hci.Reactive.IRefObservable{T},Darp.Ble.Hci.Reactive.IRefObservable{TOther})"/> operator </summary>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
/// <typeparam name="TOther"> The object that signals completion </typeparam>
file sealed class TakeUntilRefObserver<T, TOther> : IRefObserver<T>
    where T : allows ref struct
{
    private readonly IRefObserver<T> _outerObserver;
    private readonly IDisposable _otherDisposable;

    /// <summary> The implementation of the <see cref="RefObservable.Select{T,TOut}"/> operator </summary>
    /// <param name="outerObserver"> The outer observer </param>
    /// <param name="other"> A ref observable to terminate the observation </param>
    public TakeUntilRefObserver(IRefObserver<T> outerObserver, IRefObservable<TOther> other)
    {
        _outerObserver = outerObserver;
        _otherDisposable = other.Subscribe(_ => outerObserver.OnCompleted(), onError: null, onCompleted: null);
    }

    /// <summary> The implementation of the <see cref="RefObservable.Select{T,TOut}"/> operator </summary>
    /// <param name="outerObserver"> The outer observer </param>
    /// <param name="other"> An observable to terminate the observation </param>
    public TakeUntilRefObserver(IRefObserver<T> outerObserver, IObservable<TOther> other)
    {
        _outerObserver = outerObserver;
        _otherDisposable = other.Subscribe(_ => outerObserver.OnCompleted());
    }

    /// <inheritdoc />
    public void OnNext(T value) => _outerObserver.OnNext(value);

    /// <inheritdoc />
    public void OnError(Exception error) => _outerObserver.OnError(error);

    /// <inheritdoc />
    public void OnCompleted()
    {
        _outerObserver.OnCompleted();
        _otherDisposable.Dispose();
    }
}

public static partial class RefObservable
{
    /// <summary> Returns the elements from the source observable sequence until the other observable sequence produces an element </summary>
    /// <param name="source"></param>
    /// <param name="other"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TOther"></typeparam>
    /// <returns></returns>
    public static IRefObservable<T> TakeUntil<T, TOther>(this IRefObservable<T> source, IRefObservable<TOther> other)
        where T : allows ref struct
    {
        return Create<T, (IRefObservable<T> Source, IRefObservable<TOther> Other)>(
            (source, other),
            (state, observer) => state.Source.Subscribe(new TakeUntilRefObserver<T, TOther>(observer, state.Other))
        );
    }

    /// <summary> Returns the elements from the source observable sequence until the other observable sequence produces an element </summary>
    /// <param name="source"></param>
    /// <param name="other"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TOther"></typeparam>
    /// <returns></returns>
    public static IRefObservable<T> TakeUntil<T, TOther>(this IRefObservable<T> source, IObservable<TOther> other)
        where T : allows ref struct
    {
        return Create<T, (IRefObservable<T> Source, IObservable<TOther> Other)>(
            (source, other),
            (state, observer) => state.Source.Subscribe(new TakeUntilRefObserver<T, TOther>(observer, state.Other))
        );
    }
}
