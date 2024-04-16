using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble;

public interface IBleBroadcaster
{
    IDisposable Advertise(AdvertisingSet advertisingSet);
    IDisposable Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters = null);
    void Stop();
}