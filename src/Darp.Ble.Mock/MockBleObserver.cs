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

    protected override Task<IDisposable> StartObservingAsyncCore<TState>(
        TState state,
        Action<TState, IGapAdvertisement> onAdvertisement,
        CancellationToken cancellationToken
    )
    {
        IDisposable disposable = _device
            .MockedDevices.Select(x => x.GetAdvertisements(this))
            .Merge()
            .TakeUntil(_stopRequestedSubject)
            .Subscribe(advertisement => onAdvertisement(state, advertisement));
        return Task.FromResult(disposable);
    }

    protected override Task StopObservingAsyncCore()
    {
        _stopRequestedSubject.OnNext(Unit.Default);
        return Task.CompletedTask;
    }
}
