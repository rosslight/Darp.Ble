using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

internal sealed class MockBleBroadcaster : BleBroadcaster
{
    private IObservable<AdvertisingData>? _source;
    private AdvertisingParameters? _parameters;
    private CancellationTokenSource? _cancellationTokenSource;

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        BleAddress ownAddress = new(BleAddressType.Public, (UInt48)0xAABBCCDDEEFF);

        IObservable<AdvertisingData> dataSource = _source ?? Observable.Empty<AdvertisingData>();
        return dataSource
            .TakeWhile(_ => _cancellationTokenSource?.IsCancellationRequested != true)
            .Select(data => GapAdvertisement.FromExtendedAdvertisingReport(observer,
                DateTimeOffset.UtcNow,
                _parameters?.Type ?? BleEventType.None,
                ownAddress,
                Physical.Le1M,
                Physical.NotAvailable,
                AdvertisingSId.NoAdIProvided,
                (TxPowerLevel)20,
                (Rssi)(-40),
                PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                new BleAddress(BleAddressType.NotAvailable, UInt48.Zero),
                data));
    }

    /// <inheritdoc />
    protected override IDisposable AdvertiseCore(IObservable<AdvertisingData> source, AdvertisingParameters? parameters)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _source = source;
        _parameters = parameters;
        return Disposable.Create(this, self => self._source = null);
    }

    /// <inheritdoc />
    protected override void StopAllCore()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }
}