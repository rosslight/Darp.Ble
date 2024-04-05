using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

public sealed class MockBleObserver(BleBroadcasterMock broadcaster) : IPlatformSpecificBleObserver
{
    private readonly BleBroadcasterMock _broadcaster = broadcaster;

    public bool TryStartScan(BleObserver observer, out IObservable<IGapAdvertisement> observable)
    {
        observable = _broadcaster.GetAdvertisements(observer);
        return true;
    }

    public void StopScan() { }
}