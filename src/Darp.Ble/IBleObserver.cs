using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble;

/// <summary> The ble observer </summary>
public interface IBleObserver : IConnectableObservable<IGapAdvertisement>
{
    /// <summary> The ble device </summary>
    IBleDevice Device { get; }

    /// <summary> True if the observer is currently scanning </summary>
    bool IsScanning { get; }
    /// <summary> The parameters used for the current scan </summary>
    BleScanParameters Parameters { get; }

    /// <summary>
    /// Set a new configuration for advertising observation. Setting is only allowed while observer is not scanning
    /// </summary>
    /// <param name="parameters"> The configuration to set </param>
    /// <returns> True, if setting parameters was successful </returns>
    bool Configure(BleScanParameters parameters);
    /// <summary> Stop the scan that is currently running </summary>
    void StopScan();
}