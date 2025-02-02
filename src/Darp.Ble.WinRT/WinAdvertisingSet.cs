using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

/// <summary> The windows implementation of the advertising set </summary>
/// <inheritdoc />
public sealed class WinAdvertisingSet(BleBroadcaster broadcaster) : AdvertisingSet(broadcaster)
{
    /// <inheritdoc />
    public override Task SetRandomAddressAsync(
        BleAddress randomAddress,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override Task SetScanResponseDataAsync(
        AdvertisingData scanResponseData,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }
}
