using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using AdvertisingStartInfo = (Darp.Ble.Gatt.Server.IAdvertisingSet AdvertisingSet, System.TimeSpan Duration, byte NumberOfEvents);

namespace Darp.Ble;

/// <summary> The ble broadcaster </summary>
public interface IBleBroadcaster : IAsyncDisposable
{
    /// <summary> The ble device </summary>
    IBleDevice Device { get; }

    /// <summary> Creates a new advertising set </summary>
    /// <param name="parameters"> The parameters for advertising </param>
    /// <param name="data"> Optional data to advertise </param>
    /// <param name="scanResponseData"> Optional scan response data </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The created advertising set </returns>
    public Task<IAdvertisingSet> CreateAdvertisingSetAsync(
        AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default
    );

    /// <summary> Start advertising multiple advertising sets. The duration and numberOfEvents cannot both be > 0. </summary>
    /// <param name="advertisingSetStartInfo"> A collection of advertising sets together with information on how to start them </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> An async disposable to stop advertising </returns>
    public Task<IAsyncDisposable> StartAdvertisingAsync(
        IReadOnlyCollection<AdvertisingStartInfo> advertisingSetStartInfo,
        CancellationToken cancellationToken
    );
}