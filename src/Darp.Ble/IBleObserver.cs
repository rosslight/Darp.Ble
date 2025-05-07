using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;

namespace Darp.Ble;

/// <summary> The ble observer </summary>
public interface IBleObserver
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

    /// <summary> Start observing for advertisements. </summary>
    /// <param name="onAdvertisement"> The callback to be called when an advertisement event was received </param>
    /// <param name="onStopped"> The callback to be called when observation was stopped by an external factor </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial starting process </param>
    /// <returns>
    /// A task which completes when observing has started.
    /// Contains an <see cref="IAsyncDisposable"/> which can be used to unsubscribe from notifications.
    /// </returns>
    /// <exception cref="BleObservationStartException"> Thrown if the observation could not be started </exception>
    Task<IAsyncDisposable> StartObservingAsync(
        Action<IGapAdvertisement> onAdvertisement,
        Action onStopped,
        CancellationToken cancellationToken
    );

    /// <summary> Stop the scan that is currently running </summary>
    Task StopObservingAsync();
}
