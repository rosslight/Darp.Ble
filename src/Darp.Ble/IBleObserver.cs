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
    bool IsObserving { get; }

    /// <summary> The parameters used for the current scan </summary>
    BleObservationParameters Parameters { get; }

    /// <summary>
    /// Set a new configuration for advertising observation. Setting is only allowed while the observer is not scanning
    /// </summary>
    /// <param name="parameters"> The configuration to set </param>
    /// <remarks>  </remarks>
    /// <returns> True, if setting parameters was successful </returns>
    bool Configure(BleObservationParameters parameters);

    /// <summary> Register a callback called when an advertisement was received </summary>
    /// <param name="state"> A state to be passed to the callback </param>
    /// <param name="onAdvertisement"> The callback </param>
    /// <typeparam name="T"> The type of the state </typeparam>
    /// <returns> A disposable to unsubscribe the callback </returns>
    IDisposable OnAdvertisement<T>(T state, Action<T, IGapAdvertisement> onAdvertisement);

    /// <summary> Start observing for advertisements. </summary>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial starting process </param>
    /// <returns> A task which completes when observing has started. </returns>
    /// <exception cref="BleObservationStartException"> Thrown if the observation could not be started </exception>
    Task StartObservingAsync(CancellationToken cancellationToken = default);

    /// <summary> Stop the scan that is currently running </summary>
    Task StopObservingAsync();
}
