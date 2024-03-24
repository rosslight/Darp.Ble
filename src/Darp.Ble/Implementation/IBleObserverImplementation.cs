using Darp.Ble.Gap;

namespace Darp.Ble.Implementation;

/// <summary> The ble observer implementation </summary>
public interface IBleObserverImplementation
{
    /// <summary> Tries to start a new scan. </summary>
    /// <param name="observer"> The ble observer who requests the scan start </param>
    /// <param name="observable"> The observable yielding advertisements if successful or an error if unsuccessful </param>
    /// <returns> True if scan start was successful </returns>
    bool TryStartScan(BleObserver observer, out IObservable<IGapAdvertisement> observable);
    /// <summary> Stop the scan. This is not supposed to fail </summary>
    void StopScan();
}