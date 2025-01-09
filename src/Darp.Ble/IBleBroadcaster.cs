using Darp.Ble.Data;
using Darp.Ble.Gap;

using AdvertisingStartInfo = (Darp.Ble.IAdvertisingSet AdvertisingSet, System.TimeSpan Duration, byte NumberOfEvents);

namespace Darp.Ble;

public interface IAdvertisingSet : IAsyncDisposable
{
    public IBleBroadcaster Broadcaster { get; }

    public BleAddress RandomAddress { get; }
    public AdvertisingParameters Parameters { get; }
    public AdvertisingData Data { get; }
    public AdvertisingData? ScanResponseData { get; }
    public TxPowerLevel SelectedTxPower { get; }

    bool IsAdvertising { get; }

    public Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default);
    public Task SetAdvertisingParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken = default);
    public Task SetAdvertisingDataAsync(AdvertisingData data, CancellationToken cancellationToken = default);
    public Task SetScanResponseDataAsync(AdvertisingData scanResponseData, CancellationToken cancellationToken = default);
}

public interface IPeriodicAdvertisingSet : IAdvertisingSet
{
    
}

/// <summary> The ble broadcaster </summary>
public interface IBleBroadcaster : IAsyncDisposable
{
    /// <summary> Creates a new advertising set </summary>
    /// <param name="parameters"> The parameters for advertising </param>
    /// <param name="data"> Optional data to advertise </param>
    /// <param name="scanResponseData"> Optional scan response data </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The created advertising set </returns>
    public Task<IAdvertisingSet> CreateAdvertisingSetAsync(AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default);

    /// <summary> Start advertising multiple advertising sets. The duration and numberOfEvents cannot both be > 0. </summary>
    /// <param name="advertisingSet"> A collection of advertising sets together with information on how to start them </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> An async disposable to stop advertising </returns>
    public Task<IAsyncDisposable> StartAdvertisingAsync(
        IReadOnlyCollection<AdvertisingStartInfo> advertisingSet,
        CancellationToken cancellationToken
    );
}