using System.Reactive.Linq;

namespace Darp.Ble.Hci.Reactive;

/// <summary> Helpers for creating and working with ref observables </summary>
public static partial class RefObservable
{
    /// <summary> Return the current ref observable as an anonymous, ref observable </summary>
    /// <param name="source"> A sequence of elements to anonymise. </param>
    /// <typeparam name="T"> The type of the elements in the source sequence. </typeparam>
    /// <returns> An observable sequence </returns>
    public static IRefObservable<T> AsRefObservable<T>(this IRefObservable<T> source)
        where T : allows ref struct
    {
        return Create<T, IRefObservable<T>>(source, (state, observer) => state.Subscribe(observer));
    }

    /// <summary> Return the current observable as a ref observable </summary>
    /// <param name="source"> A sequence of elements to anonymise. </param>
    /// <typeparam name="T"> The type of the elements in the source sequence. </typeparam>
    /// <returns> An observable sequence </returns>
    public static IRefObservable<T> AsRefObservable<T>(this IObservable<T> source)
    {
        return Create<T, IObservable<T>>(
            source,
            (state, observer) => state.Subscribe(observer.OnNext, observer.OnError, observer.OnCompleted)
        );
    }

    public static IObservable<T> AsObservable<T>(this IRefObservable<T> source)
    {
        return Observable.Create<T>(observer =>
            source.Subscribe(observer.OnNext, observer.OnError, observer.OnCompleted)
        );
    }

    public static IObservable<TOut> AsObservable<T, TOut>(this IRefObservable<T> source, Func<T, TOut> selector)
        where T : allows ref struct
    {
        return Observable.Create<TOut>(observer =>
            source.Subscribe(value => observer.OnNext(selector(value)), observer.OnError, observer.OnCompleted)
        );
    }
}
