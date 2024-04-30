using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleObserver(MockBleDevice device, MockBleBroadcaster broadcaster, ILogger? logger) : BleObserver(device, logger)
{
    private readonly MockBleBroadcaster _broadcaster = broadcaster;

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        observable = _broadcaster.GetAdvertisements(this);
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
    }
}