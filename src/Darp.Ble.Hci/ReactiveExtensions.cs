using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Reactive;

namespace Darp.Ble.Hci;

/// <summary> Class holding different reactive extensions </summary>
public static class ReactiveExtensions
{
    private static IObservable<T> TakeUntil<T>(this IObservable<T> source, CancellationToken cancellationToken)
    {
        IObservable<T> cancelObservable = Observable.Create<T>(observer =>
            cancellationToken.Register(() =>
            {
                observer.OnError(new OperationCanceledException(cancellationToken));
            })
        );
        return source.TakeUntil(cancelObservable);
    }

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

    /// <summary> Select everything, where the <paramref name="trySelector"/> returned true </summary>
    /// <param name="source"> The source to operate on </param>
    /// <param name="trySelector"> The selector if return is true </param>
    /// <typeparam name="T"> The type of the source </typeparam>
    /// <typeparam name="TResult"> The type of the result </typeparam>
    /// <returns> The resulting observable </returns>
    public static IRefObservable<TResult> SelectWhere<T, TResult>(
        this IRefObservable<T> source,
        TrySelector<T, TResult> trySelector
    )
        where T : allows ref struct
        where TResult : allows ref struct
    {
        return RefObservable.Create<TResult>(observer =>
        {
            return source.Subscribe(
                next =>
                {
                    try
                    {
                        if (trySelector(next, out TResult? result))
                            observer.OnNext(result);
                    }
                    catch (Exception e)
                    {
                        observer.OnError(e);
                    }
                },
                observer.OnError,
                observer.OnCompleted
            );
        });
    }

    /// <summary> Select all event packets which where the event data matches <typeparamref name="TEvent"/> </summary>
    /// <param name="source"> The source to operate on </param>
    /// <typeparam name="TEvent"> The type of the event data </typeparam>
    /// <returns> An observable with the event data </returns>
    public static IRefObservable<HciEventPacket<TEvent>> SelectWhereEvent<TEvent>(
        this IRefObservable<HciEventPacket> source
    )
        where TEvent : IHciEvent<TEvent> =>
        source.SelectWhere<HciEventPacket, HciEventPacket<TEvent>>(HciEventPacket.TryWithData);

    /// <summary> Select all event packets which where the le event data matches <typeparamref name="TLeMetaEvent"/> </summary>
    /// <param name="source"> The source to operate on </param>
    /// <typeparam name="TLeMetaEvent"> The type of the event data </typeparam>
    /// <returns> An observable with the le event data </returns>
    public static IObservable<HciEventPacket<TLeMetaEvent>> SelectWhereLeMetaEvent<TLeMetaEvent>(
        this IObservable<HciEventPacket<HciLeMetaEvent>> source
    )
        where TLeMetaEvent : IHciLeMetaEvent<TLeMetaEvent>
    {
        return Observable.Create<HciEventPacket<TLeMetaEvent>>(observer =>
        {
            return source
                .Where(x => x.Data.SubEventCode == TLeMetaEvent.SubEventType)
                .Subscribe(
                    next =>
                    {
                        if (HciEventPacket.TryWithData(next, out HciEventPacket<TLeMetaEvent>? result))
                            observer.OnNext(result);
                    },
                    observer.OnError,
                    observer.OnCompleted
                );
        });
    }
}
