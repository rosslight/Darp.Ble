using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Examples.Unix.Mockup;

internal sealed class BMObserver(BleDevice device, BMBroadcaster broadcaster, ILogger? logger) : BleObserver(device, logger)
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