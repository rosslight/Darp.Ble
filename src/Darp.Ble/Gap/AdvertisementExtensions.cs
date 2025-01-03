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
    public static IObservable<IGattServerPeer> Connect(this IGapAdvertisement advertisement,
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

    /// <summary> Call <see cref="Connect"/> on a source of advertisements </summary>
    /// <param name="source"> The source of advertisements to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <returns> An observable of the connection </returns>
    public static IObservable<IGattServerPeer> ConnectToPeripheral(this IObservable<IGapAdvertisement> source,
        BleConnectionParameters? connectionParameters = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.SelectMany(x => x.Connect(connectionParameters));
    }
}