using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace Darp.Ble.Hci;

/// <summary> Class holding different reactive extensions </summary>
public static class ReactiveExtensions
{
    /// <summary> The selector </summary>
    /// <typeparam name="T"> The type of the source </typeparam>
    /// <typeparam name="TResult"> The type of the result </typeparam>
    public delegate bool TrySelector<in T, TResult>(T value, [NotNullWhen(true)] out TResult? result)
        where T : allows ref struct
        where TResult : allows ref struct;

    /// <summary> Select everything, where the <paramref name="trySelector"/> returned true </summary>
    /// <param name="source"> The source to operate on </param>
    /// <param name="trySelector"> The selector if return is true </param>
    /// <typeparam name="T"> The type of the source </typeparam>
    /// <typeparam name="TResult"> The type of the result </typeparam>
    /// <returns> The resulting observable </returns>
    public static IObservable<TResult> SelectWhere<T, TResult>(
        this IObservable<T> source,
        TrySelector<T, TResult> trySelector
    )
    {
        return Observable.Create<TResult>(observer =>
        {
            return source.Subscribe(
                next =>
                {
                    try
                    {
                        if (trySelector(next, out TResult? result))
                            observer.OnNext(result);
                    }
#pragma warning disable CA1031
                    catch (Exception e)
                    {
                        observer.OnError(e);
                    }
#pragma warning restore CA1031
                },
                observer.OnError,
                observer.OnCompleted
            );
        });
    }
}
