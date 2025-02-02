using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gap;

/// <summary> Advertisement specific exceptions </summary>
public static class AdvertisementExtensions
{
    /// <summary> Connect to an advertisement </summary>
    /// <param name="advertisement"> The advertisement to connect to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <returns> An observable of the connection </returns>
    public static IObservable<IGattServerPeer> ConnectToPeripheral(this IGapAdvertisement advertisement,
        BleConnectionParameters? connectionParameters = null)
    {
        return Observable.Create<IGattServerPeer>(observer =>
        {
            if (!advertisement.EventType.HasFlag(BleEventType.Connectable))
            {
                observer.OnError(new BleAdvertisementException(advertisement,
                    "Unable to connect to advertisement as it is not connectable"));
                return Disposable.Empty;
            }
            IBleDevice device = advertisement.Observer.Device;
            if (!device.Capabilities.HasFlag(Capabilities.Central))
            {
                observer.OnError(new BleDeviceException(device,
                    "Unable to connect to advertisement as it was captured by a device which does not support connections"));
                return Disposable.Empty;
            }
            IBleCentral central = device.Central;
            return central.ConnectToPeripheral(advertisement.Address, connectionParameters, scanParameters: null)
                .Subscribe(observer);
        });
    }

    /// <summary> Call <see cref="ConnectToPeripheral(Darp.Ble.Gap.IGapAdvertisement,Darp.Ble.Data.BleConnectionParameters?)"/> on a source of advertisements </summary>
    /// <param name="source"> The source of advertisements to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <returns> An observable of the connection </returns>
    public static IObservable<IGattServerPeer> ConnectToPeripheral(this IObservable<IGapAdvertisement> source,
        BleConnectionParameters? connectionParameters = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.SelectMany(x => x.ConnectToPeripheral(connectionParameters));
    }

    /// <summary>
    /// Filters the elements of an observable sequence of advertisements based on a given address on type and address value.
    /// If <paramref name="address"/> is null, no filter will be applied!
    /// </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <param name="address">The address to test each source advertisement against.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence that match the address.</returns>
    public static IObservable<TAdv> WhereAddress<TAdv>(this IObservable<TAdv> source, BleAddress? address)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return address is null
            ? source
            : source.Where(adv => adv.Address.Equals(address));
    }

    /// <summary>
    /// Filters the elements of an observable sequence of advertisements based on a given address value.
    /// If <paramref name="address"/> is null, no filter will be applied!
    /// </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <param name="address">The address to test each source advertisement against.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence that match the address value.</returns>
    public static IObservable<TAdv> WhereAddress<TAdv>(this IObservable<TAdv> source, ulong? address)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return address is null
            ? source
            : source.Where(adv => adv.Address.Equals(address.Value));
    }

    /// <summary> Filters the elements of an observable sequence of advertisements based on a given advertisement type. </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <param name="type">The pdu type to test each source advertisement against.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence that match the advertisement type.</returns>
    public static IObservable<TAdv> WhereType<TAdv>(this IObservable<TAdv> source, BleEventType type)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType == type);
    }

    /// <summary> Filters the elements of an observable sequence of advertisements when they are <see cref="BleEventType.Connectable"/>. </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence which are connectable.</returns>
    public static IObservable<TAdv> WhereConnectable<TAdv>(this IObservable<TAdv> source)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType.HasFlag(BleEventType.Connectable));
    }

    /// <summary> Filters the elements of an observable sequence of advertisements when they are NOT <see cref="BleEventType.Connectable"/>. </summary>
    /// <param name="source"> An observable sequence of advertisements whose elements to filter. </param>
    /// <typeparam name="TAdv"> The type of the advertisements in the source sequence. </typeparam>
    /// <returns> An observable sequence of advertisements that contains elements from the input sequence which are NOT connectable. </returns>
    public static IObservable<TAdv> WhereUnconnectable<TAdv>(this IObservable<TAdv> source)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => !adv.EventType.HasFlag(BleEventType.Connectable));
    }

    /// <summary> Filters the elements of an observable sequence of advertisements when they are <see cref="BleEventType.Directed"/>. </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence which are directed.</returns>
    public static IObservable<TAdv> WhereDirected<TAdv>(this IObservable<TAdv> source)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType.HasFlag(BleEventType.Directed));
    }

    /// <summary> Filters the elements of an observable sequence of advertisements when they are <see cref="BleEventType.Scannable"/>. </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence which are scannable.</returns>
    public static IObservable<TAdv> WhereScannable<TAdv>(this IObservable<TAdv> source)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType.HasFlag(BleEventType.Scannable));
    }

    /// <summary> Filters the elements of an observable sequence of advertisements when they are <see cref="BleEventType.ScanResponse"/>. </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence which are scan responses.</returns>
    public static IObservable<TAdv> WhereScanResponse<TAdv>(this IObservable<TAdv> source)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType.HasFlag(BleEventType.ScanResponse));
    }

    /// <summary>
    /// Filters the elements of an observable sequence of advertisements based on a given <see cref="Guid"/> service.
    /// If <paramref name="service"/> is null, no filter will be applied!
    /// </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <param name="service">The service <see cref="Guid"/> to test each source advertisement against.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence that contains the service uuid.</returns>
    public static IObservable<TAdv> WhereService<TAdv>(
        this IObservable<TAdv> source,
        BleUuid? service)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return service is null
            ? source
            : source.Where(x => x.Data.GetServiceUuids().Any(uuid => uuid.Equals(service.Value)));
    }

    /// <summary>
    /// Filters the elements of an observable sequence of advertisements based on a given service ushort uuid.
    /// If <paramref name="service"/> is null, no filter will be applied!
    /// </summary>
    /// <param name="source">An observable sequence of advertisements whose elements to filter.</param>
    /// <param name="service">The service ushort uuid to test each source advertisement against.</param>
    /// <typeparam name="TAdv">The type of the advertisements in the source sequence.</typeparam>
    /// <returns>An observable sequence of advertisements that contains elements from the input sequence that contains the service uuid.</returns>
    public static IObservable<TAdv> WhereService<TAdv>(
        this IObservable<TAdv> source,
        ushort? service)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return service is null
            ? source
            : source.Where(x => x.Data.GetServiceUuids().Any(uuid => uuid.Equals(service.Value)));
    }
}