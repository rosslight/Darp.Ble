using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Implementation;

/// <summary> The default implementation for an advertising set </summary>
/// <param name="broadcaster"> The broadcaster of this advertisment </param>
/// <param name="randomAddress"> The initial random address </param>
/// <param name="parameters"> The initial advertising parameters </param>
/// <param name="data"> The initial advertising data </param>
/// <param name="scanResponseData"> The initial scan response data </param>
/// <param name="selectedTxPower"> The initial tx power </param>
public abstract class AdvertisingSet(IBleBroadcaster broadcaster,
    BleAddress randomAddress,
    AdvertisingParameters parameters,
    AdvertisingData data,
    AdvertisingData scanResponseData,
    TxPowerLevel selectedTxPower
) : IAdvertisingSet
{
    /// <inheritdoc />
    public IBleBroadcaster Broadcaster { get; } = broadcaster;
    /// <inheritdoc />
    public BleAddress RandomAddress { get; protected set; } = randomAddress;
    /// <inheritdoc />
    public AdvertisingParameters Parameters { get; protected set; } = parameters;
    /// <inheritdoc />
    public AdvertisingData Data { get; protected set; } = data;
    /// <inheritdoc />
    public AdvertisingData ScanResponseData { get; protected set; } = scanResponseData;
    /// <inheritdoc />
    public TxPowerLevel SelectedTxPower { get; } = selectedTxPower;

    /// <inheritdoc />
    public bool IsAdvertising { get; }

    /// <inheritdoc />
    public virtual Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default)
    {
        RandomAddress = randomAddress;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetAdvertisingParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken = default)
    {
        Parameters = parameters;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetAdvertisingDataAsync(AdvertisingData data, CancellationToken cancellationToken = default)
    {
        Data = data;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task SetScanResponseDataAsync(AdvertisingData scanResponseData, CancellationToken cancellationToken = default)
    {
        ScanResponseData = scanResponseData;
        return Task.CompletedTask;
    }
}