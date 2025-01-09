using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

/// <summary> The windows implementation of the advertising set </summary>
/// <inheritdoc />
public sealed class WinAdvertisingSet(IBleBroadcaster broadcaster,
    BleAddress randomAddress,
    AdvertisingParameters parameters,
    AdvertisingData data,
    AdvertisingData? scanResponseData,
    TxPowerLevel selectedTxPower)
    : AdvertisingSet(broadcaster, randomAddress, parameters, data, scanResponseData, selectedTxPower)
{
    /// <inheritdoc />
    public override Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override Task SetScanResponseDataAsync(AdvertisingData scanResponseData, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}