using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Gatt.Server;

/// <summary> An advertising set </summary>
public interface IAdvertisingSet : IAsyncDisposable
{
    /// <summary> The broadcaster advertising this set </summary>
    public IBleBroadcaster Broadcaster { get; }

    /// <summary> The random address to be used when advertising </summary>
    public BleAddress RandomAddress { get; }

    /// <summary> The advertising parameters </summary>
    public AdvertisingParameters Parameters { get; }

    /// <summary> The advertising data </summary>
    public AdvertisingData Data { get; }

    /// <summary> The optional scan response data </summary>
    public AdvertisingData? ScanResponseData { get; }

    /// <summary> The actual tx power level selected by the controller </summary>
    public TxPowerLevel SelectedTxPower { get; }

    /// <summary> Indication on whether the set is currently advertising </summary>
    bool IsAdvertising { get; }

    /// <summary> Sets a new random address </summary>
    /// <param name="randomAddress"> The random address to be used </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which completes when the address change was requested </returns>
    public Task SetRandomAddressAsync(
        BleAddress randomAddress,
        CancellationToken cancellationToken = default
    );

    /// <summary> Sets new advertising parameters </summary>
    /// <param name="parameters"> The parameters to be set </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which completes when the parameters were set </returns>
    public Task SetAdvertisingParametersAsync(
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    );

    /// <summary> Sets new advertising data </summary>
    /// <param name="data"> The data to be set </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which completes when the data was updated </returns>
    public Task SetAdvertisingDataAsync(
        AdvertisingData data,
        CancellationToken cancellationToken = default
    );

    /// <summary> Sets new scan response data </summary>
    /// <param name="scanResponseData"> The scan response data to be set </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which completes when the scan response data was updated </returns>
    public Task SetScanResponseDataAsync(
        AdvertisingData scanResponseData,
        CancellationToken cancellationToken = default
    );
}

/// <summary> A periodic advertising set </summary>
public interface IPeriodicAdvertisingSet : IAdvertisingSet { }
