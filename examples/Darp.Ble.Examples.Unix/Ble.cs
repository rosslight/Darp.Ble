using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Mock;

namespace Darp.Ble.Examples.Unix;

internal sealed class Ble : IDisposable
{
    private IBleObserver? m_observer;
    private IDisposable? m_unsubscriber;
    private AdvGenerator? m_generator;

    public async Task StartScanAsync(IBleDevice adapter, Action<IGapAdvertisement> onNextAdvertisement)
    {
        StopScan();

        m_generator = new AdvGenerator();

        await adapter.InitializeAsync();
        m_observer = adapter.Observer;

        m_observer.Configure(new BleScanParameters()
        {
            ScanType = ScanType.Active,
            ScanWindow = ScanTiming.Ms100,
            ScanInterval = ScanTiming.Ms100,
        });

        m_unsubscriber = m_observer.Subscribe(onNextAdvertisement);

        m_observer.Connect();
    }

    public void StopScan()
    {
        m_unsubscriber?.Dispose();
        m_unsubscriber = null;

        m_observer?.StopScan();
        m_observer = null;

        m_generator?.Stop();
        m_generator?.Dispose();
        m_generator = null;
    }

    public Task Initialize(IBleBroadcaster broadcaster, IBlePeripheral peripheral)
    {
        if (broadcaster is IMockBleBroadcaster mockBroadcaster)
        {
            mockBroadcaster.OnGetAdvertisements = GetAdvertisements;
        }

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
                (TxPowerLevel)20,
                (Rssi)(-40),
                PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                new BleAddress(UInt48.Zero),
                x.Data));
    }

    public void Dispose()
    {
        m_generator?.Dispose();
    }
}
