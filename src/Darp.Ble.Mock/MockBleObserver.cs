using Darp.Ble.Gap;
using Darp.Ble.Logger;

namespace Darp.Ble.Mock;

internal sealed class MockBleObserver(MockBleDevice device, BleBroadcasterMock broadcaster, IObserver<LogEvent>? logger) : BleObserver(device, logger)
{
    private readonly BleBroadcasterMock _broadcaster = broadcaster;

    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        observable = _broadcaster.GetAdvertisements(this);
        return true;
    }

    protected override void StopScanCore()
    {
    }
}