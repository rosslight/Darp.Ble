using Darp.Ble.Gap;

namespace Darp.Ble.Backends;

/// <summary> The advertising of an adapter </summary>
public interface IBroadcasterDriver
{
    /// <summary> Creates a stopped advertising set </summary>
    /// <param name="parameters"> The validated parameters; the driver rounds the intervals to the controller's units </param>
    /// <param name="data"> The advertising data </param>
    /// <param name="scanResponseData"> The scan response data, if the set has one </param>
    /// <param name="sink"> Receives what happens to the set without the core asking </param>
    /// <param name="cancellationToken"> Cancels creating </param>
    /// <returns> The set; the core owns it and disposes it once </returns>
    Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    );
}
