using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

public interface IMockBleBroadcaster
{
    public delegate IObservable<IGapAdvertisement> Delegate_OnGetAdvertisements(BleObserver observer, AdvertisingParameters? parameters, CancellationTokenSource? cancellationTokenSource);
    public Delegate_OnGetAdvertisements? OnGetAdvertisements { get; set; }
}