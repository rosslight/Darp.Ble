using System.Reactive.Linq;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleObserver(MockBleDevice device, ILogger? logger) : BleObserver(device, logger)
{
    private readonly MockBleDevice _device = device;

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        observable = _device.MockedPeripherals.Select(x => x.GetAdvertisements(this)).Merge();
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
    }
}