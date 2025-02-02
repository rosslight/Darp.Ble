using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
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

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        return _advertisingSetPublishedSubject.Select(set =>
            GapAdvertisement.FromExtendedAdvertisingReport(
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
            )
        );
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
            await advertisingSet.SetAdvertisingDataAsync(scanResponseData, cancellationToken).ConfigureAwait(false);
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
                .Select(_ => advertisingSet);
            disposables.Add(observable.Subscribe(_advertisingSetPublishedSubject));
        }
#pragma warning disable CA2000 // Will be disposed when async disposable is disposed
        IAsyncDisposable asyncDisposable = AsyncDisposable.Create(new CompositeDisposable(disposables));
#pragma warning restore CA2000
        return Task.FromResult(asyncDisposable);
    }
}

internal sealed class MockAdvertisingSet(MockedBleBroadcaster broadcaster) : AdvertisingSet(broadcaster) { }
