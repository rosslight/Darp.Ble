using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleBroadcaster(ILogger? logger) : BleBroadcaster(logger), IMockBleBroadcaster
{
    private IObservable<AdvertisingData>? _source;
    private AdvertisingParameters? _parameters;
    private CancellationTokenSource? _cancellationTokenSource;
    public IMockBleBroadcaster.Delegate_OnGetAdvertisements? OnGetAdvertisements { get; set;}

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        if (OnGetAdvertisements != null)
            return OnGetAdvertisements(observer, _parameters, _cancellationTokenSource);

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
                BleAddress.NotAvailable,
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

    protected override IDisposable AdvertiseCore(AdvertisingData data, TimeSpan interval, AdvertisingParameters? parameters)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override void StopAllCore()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }
}