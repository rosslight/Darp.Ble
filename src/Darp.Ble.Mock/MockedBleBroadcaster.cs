using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockedBleBroadcaster(MockedBleDevice bleDevice, ILogger<MockedBleBroadcaster> logger)
    : BleBroadcaster(bleDevice, logger)
{
    private readonly MockedBleDevice _device = bleDevice;
    private readonly Subject<IAdvertisingSet> _advertisingSetPublishedSubject = new();
    private readonly Subject<Unit> _stopRequestedSubject = new();

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer, ScanType observerScanType)
    {
        return _advertisingSetPublishedSubject.SelectMany(set =>
        {
            GapAdvertisement advReport = GapAdvertisement.FromExtendedAdvertisingReport(
                observer,
                DateTimeOffset.UtcNow,
                set.Parameters.Type,
                set.RandomAddress,
                set.Parameters.PrimaryPhy,
                Physical.NotAvailable,
                set.Parameters.AdvertisingSId,
                set.SelectedTxPower,
                _device.Settings.TxPowerToRssi(set.SelectedTxPower),
                PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                BleAddress.NotAvailable,
                set.Data
            );
            IObservable<GapAdvertisement> observable = Observable.Return(advReport);
            if (observerScanType is not ScanType.Active || !set.Parameters.Type.HasFlag(BleEventType.Scannable))
            {
                return observable;
            }
            IObservable<GapAdvertisement> scanResponseObservable = Observable
                .Timer(TimeSpan.FromMilliseconds(4.2))
                .Select(_ =>
                    GapAdvertisement.FromExtendedAdvertisingReport(
                        observer,
                        DateTimeOffset.UtcNow,
                        BleEventType.ScanResponse,
                        set.RandomAddress,
                        set.Parameters.PrimaryPhy,
                        Physical.NotAvailable,
                        set.Parameters.AdvertisingSId,
                        set.SelectedTxPower,
                        _device.Settings.TxPowerToRssi(set.SelectedTxPower),
                        PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                        BleAddress.NotAvailable,
                        set.ScanResponseData ?? AdvertisingData.Empty
                    )
                );
            return observable.Merge(scanResponseObservable);
        });
    }

    protected override async Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(
        AdvertisingParameters? parameters,
        AdvertisingData? data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken
    )
    {
        var advertisingSet = new MockAdvertisingSet(this);
        await advertisingSet.SetRandomAddressAsync(_device.RandomAddress, cancellationToken).ConfigureAwait(false);
        if (parameters is not null)
        {
            await advertisingSet.SetAdvertisingParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
        }
        if (data is not null)
        {
            await advertisingSet.SetAdvertisingDataAsync(data, cancellationToken).ConfigureAwait(false);
        }
        if (scanResponseData is not null)
        {
            await advertisingSet.SetScanResponseDataAsync(scanResponseData, cancellationToken).ConfigureAwait(false);
        }
        return advertisingSet;
    }

    protected override Task<IAsyncDisposable> StartAdvertisingCoreAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSets,
        CancellationToken cancellationToken
    )
    {
        List<IDisposable> disposables = [];
        foreach ((IAdvertisingSet advertisingSet, _, _) in advertisingSets)
        {
            TimeSpan minInterval = TimeSpan.FromMilliseconds(
                (ushort)advertisingSet.Parameters.MinPrimaryAdvertisingInterval / 1.6
            );
            TimeSpan maxInterval = TimeSpan.FromMilliseconds(
                (ushort)advertisingSet.Parameters.MaxPrimaryAdvertisingInterval / 1.6
            );
            IObservable<IAdvertisingSet> observable = Observable
                .Interval((minInterval + maxInterval) / 2, _device.Scheduler)
                .TakeUntil(_stopRequestedSubject)
                .Select(_ => advertisingSet);
            disposables.Add(observable.Subscribe(_advertisingSetPublishedSubject));
        }
#pragma warning disable CA2000 // Will be disposed when async disposable is disposed
        IAsyncDisposable asyncDisposable = AsyncDisposable.Create(new CompositeDisposable(disposables));
#pragma warning restore CA2000
        return Task.FromResult(asyncDisposable);
    }

    protected override Task<bool> StopAdvertisingCoreAsync(
        IReadOnlyCollection<IAdvertisingSet> advertisingSets,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        _stopRequestedSubject.OnNext(Unit.Default);
        base.Dispose(disposing);
    }
}

internal sealed class MockAdvertisingSet(MockedBleBroadcaster broadcaster) : AdvertisingSet(broadcaster);
