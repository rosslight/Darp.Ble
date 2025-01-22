using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

/// <summary> The mock ble broadcaster </summary>
public interface IMockBleBroadcaster : IBleBroadcaster
{
    /// <summary> The definition of delegate for OnGetAdvertisements </summary>
    public delegate IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer, AdvertisingParameters? parameters, CancellationTokenSource? cancellationTokenSource);

    /// <summary> The delegate for OnGetAdvertisements </summary>
    public GetAdvertisements? OnGetAdvertisements { get; set; }
}