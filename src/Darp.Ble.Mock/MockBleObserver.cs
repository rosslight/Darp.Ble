using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleObserver(MockBleDevice device, ILogger<MockBleObserver> logger)
    : BleObserver(device, logger)
{
    private readonly MockBleDevice _device = device;
    private readonly Subject<Unit> _stopRequestedSubject = new();
    private IDisposable? _observableSubscription;

    protected override Task StartObservingAsyncCore(CancellationToken cancellationToken)
    {
        _observableSubscription = _device
            .MockedDevices.Select(x => x.GetAdvertisements(this, Parameters.ScanType))
            .Merge()
            .TakeUntil(_stopRequestedSubject)
            .Subscribe(OnNext);
        return Task.CompletedTask;
    }

    protected override Task StopObservingAsyncCore()
    {
        _stopRequestedSubject.OnNext(Unit.Default);
        _observableSubscription?.Dispose();
        return Task.CompletedTask;
    }
}
