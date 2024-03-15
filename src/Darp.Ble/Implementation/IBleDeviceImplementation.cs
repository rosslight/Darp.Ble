using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Implementation;

/// <summary> The ble device implementation </summary>
public interface IBleDeviceImplementation
{
    /// <summary> Initializes the ble device. </summary>
    /// <returns> The status of the initialization. Success or a custom error code. </returns>
    Task<InitializeResult> InitializeAsync();
    /// <summary> Get access to the implementation specific observer </summary>
    IBleObserverImplementation? Observer { get; }
}

/// <summary> The ble observer implementation </summary>
public interface IBleObserverImplementation
{
    /// <summary> Tries to start a new scan. </summary>
    /// <param name="observable"> The observable yielding advertisements if successful or an error if unsuccessful </param>
    /// <returns> True if scan start was successful </returns>
    bool TryStartScan(out IObservable<IGapAdvertisement> observable);
    /// <summary> Stop the scan. This is not supposed to fail </summary>
    void StopScan();
}