using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

public interface IMockBleBroadcaster : IBleBroadcaster
{
    public delegate IObservable<IGapAdvertisement> OnGetAdvertisementsDelegate(BleObserver observer, AdvertisingParameters? parameters, CancellationTokenSource? cancellationTokenSource);
    public OnGetAdvertisementsDelegate? OnGetAdvertisements { get; set; }
}