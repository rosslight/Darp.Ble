using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci;

public static class ReactiveExtensions
{
    public static IObservable<T> TakeUntil<T>(this IObservable<T> source, CancellationToken cancellationToken)
    {
        IObservable<T> cancelObservable = Observable.Create<T>(observer => cancellationToken.Register(() =>
        {
            observer.OnError(new OperationCanceledException(cancellationToken));
        }));
        return source.TakeUntil(cancelObservable);
    }

    /// <summary>
    /// Try to send a command. See BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E, 5.4.2
    /// </summary>
    /// <param name="hciHost">The host</param>
    /// <param name="aclData">The acl data to be sent</param>
    /// <typeparam name="TAcl">The type of the adl packet to be sent</typeparam>
    /// <returns>True if command was queued successfully</returns>
    public static void SendAcl<TAcl>(this HciHost hciHost, TAcl aclData) where TAcl : IEncodable
    {
    }

    public delegate bool TrySelector<in T, TResult>(T value, [NotNullWhen(true)] out TResult? result);

    public static IObservable<TResult> SelectWhere<T, TResult>(this IObservable<T> source,
        TrySelector<T, TResult> trySelector)
    {
        return Observable.Create<TResult>(observer =>
        {
            return source.Subscribe(next =>
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
            }, observer.OnError, observer.OnCompleted);
        });
    }

    public static IObservable<HciEventPacket<TEvent>> SelectWhereEvent<TEvent>(this IObservable<HciEventPacket> source)
        where TEvent : IHciEvent<TEvent> =>
        source.SelectWhere<HciEventPacket, HciEventPacket<TEvent>>(HciEventPacket.TryWithData);

    public static IObservable<HciEventPacket<TLeMetaEvent>> SelectWhereLeMetaEvent<TLeMetaEvent>(this IObservable<HciEventPacket<HciLeMetaEvent>> source)
        where TLeMetaEvent : IHciLeMetaEvent<TLeMetaEvent>
    {
        return Observable.Create<HciEventPacket<TLeMetaEvent>>(observer =>
        {
            return source
                .Where(x => x.Data.SubEventCode == TLeMetaEvent.SubEventType)
                .Subscribe(next =>
                {
                    if (HciEventPacket.TryWithData(next, out HciEventPacket<TLeMetaEvent>? result))
                        observer.OnNext(result);
                }, observer.OnError, observer.OnCompleted);
        });
    }
}