using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Mock;

namespace Darp.Ble.Examples.Unix;

internal sealed class Ble : IDisposable
{
    private IBleObserver? m_observer;
    private IBleDevice? m_adapter;
    private IDisposable? m_subscriptionForObserver;
    private IDisposable? m_subscriptionForConnect;
    private AdvGenerator? m_generator;

    public async Task StartScanAsync(IBleDevice adapter, Action<IGapAdvertisement> onNextAdvertisement)
    {
        StopScan();

        m_generator = new AdvGenerator();

        m_adapter = adapter;

        await adapter.InitializeAsync();
        m_observer = adapter.Observer;
        m_observer.Configure(new BleScanParameters()
        {
            ScanType = ScanType.Active,
            ScanWindow = ScanTiming.Ms100,
            ScanInterval = ScanTiming.Ms100,
        });
        m_subscriptionForObserver = m_observer.Subscribe(onNextAdvertisement);

        m_subscriptionForConnect = m_observer.Connect();
    }

    public void StopScan()
    {
        m_subscriptionForConnect?.Dispose();
        m_subscriptionForConnect = null;

        m_subscriptionForObserver?.Dispose();
        m_subscriptionForObserver = null;

        m_observer?.StopScan();
        m_observer = null;

        m_adapter?.DisposeAsync().AsTask().Wait();
        m_adapter = null;

        m_generator?.Stop();
        m_generator?.Dispose();
        m_generator = null;
    }

    public void Dispose()
    {
        StopScan();
    }

    public Task Initialize(IMockBleBroadcaster broadcaster, IBlePeripheral peripheral)
    {
        broadcaster.OnGetAdvertisements = GetAdvertisements;
        return Task.CompletedTask;
    }

    private IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer, AdvertisingParameters? parameters, CancellationTokenSource? cancellationTokenSource)
    {
        IObservable<AdvGenerator.DataExt> source = m_generator ?? Observable.Empty<AdvGenerator.DataExt>();
        return source
            .TakeWhile(_ => cancellationTokenSource?.IsCancellationRequested != true)
            .Select(x => GapAdvertisement.FromExtendedAdvertisingReport(
                observer,
                DateTimeOffset.UtcNow,
                parameters?.Type ?? BleEventType.None,
                x.Address,
                Physical.Le1M,
                Physical.NotAvailable,
                AdvertisingSId.NoAdIProvided,
                x.TxPower,
                x.Rssi,
                PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                new BleAddress(UInt48.Zero),
                x.Data));
    }
}
