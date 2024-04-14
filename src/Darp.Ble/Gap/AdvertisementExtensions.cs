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
    public static IObservable<GattServerPeer> Connect(this IGapAdvertisement advertisement,
        BleConnectionParameters? connectionParameters = null)
    {
        return Observable.Create<GattServerPeer>(observer =>
        {
            if (!advertisement.EventType.HasFlag(BleEventType.Connectable))
            {
                observer.OnError(new BleAdvertisementException(advertisement,
                    "Unable to connect to advertisement as it is not connectable"));
                return Disposable.Empty;
            }
            BleDevice device = advertisement.Observer.Device;
            if (device.Capabilities.HasFlag(Capabilities.Central))
            {
                observer.OnError(new BleDeviceException(device,
                    "Unable to connect to advertisement as it was captured by a device which does not support connections"));
                return Disposable.Empty;
            }
            BleCentral central = device.Central;
            return central.ConnectToPeripheral(advertisement.Address, connectionParameters, scanParameters: null)
                .Subscribe(observer);
        });
    }

    public static IObservable<GattServerPeer> ConnectToPeripheral<TAdv>(this IObservable<TAdv> source,
        BleConnectionParameters? connectionParameters = null)
        where TAdv : IGapAdvertisement
    {
        return source.SelectMany(x => x.Connect(connectionParameters));
    }
}