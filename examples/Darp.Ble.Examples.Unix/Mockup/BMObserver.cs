using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Examples.Unix.Mockup;

internal class BMObserver(BleDevice device, BMBroadcaster broadcaster, IObserver<LogEvent>? logger) : BleObserver(device, logger)
{
    private readonly BMBroadcaster m_broadcaster = broadcaster;

    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        observable = m_broadcaster.GetAdvertisements(this);
        return true;
    }

    protected override void StopScanCore()
    {
        m_broadcaster.StopAll();
    }
}