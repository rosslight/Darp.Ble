using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;

namespace Darp.Ble.Gap;

/// <summary> Advertisement specific exceptions </summary>
public static class AdvertisementExtensions
{
    /// <summary> Connect to an advertisement </summary>
    /// <param name="advertisement"> The advertisement to connect to </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    /// <returns> An observable of the connection </returns>
    public static IObservable<object> Connect(this IGapAdvertisement advertisement, BleScanParameters scanParameters)
    {
        return Observable.Create<object>(observer =>
        {
            if (!advertisement.EventType.HasFlag(BleEventType.Connectable))
            {
                observer.OnError(new BleAdvertisementException(advertisement,
                    "Unable to connect to advertisement as it is not connectable"));
                return Disposable.Empty;
            }
            if (advertisement.Observer.Device.Capabilities.HasFlag(Capabilities.Central))
            {
                observer.OnError(new BleDeviceException(advertisement.Observer.Device,
                    "Unable to connect to advertisement as it was captured by a device which does not support connections"));
                return Disposable.Empty;
            }
            BleCentral central = advertisement.Observer.Device.Central;
            throw new NotImplementedException();
            return Disposable.Empty;
        });

    }
}