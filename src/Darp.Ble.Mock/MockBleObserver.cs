using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleObserver(MockBleDevice device, ILogger<MockBleObserver> logger) : BleObserver(device, logger)
{
    private readonly MockBleDevice _device = device;
    private readonly Subject<Unit> _stopRequestedSubject = new();

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        observable = _device.MockedDevices
            .Select(x => x.GetAdvertisements(this))
            .Merge()
            .TakeUntil(_stopRequestedSubject);
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
        _stopRequestedSubject.OnNext(Unit.Default);
    }
}