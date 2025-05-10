using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock;

namespace Darp.Ble.Examples.Unix;

internal sealed class Ble : IDisposable
{
    private IBleObserver? m_observer;
    private IBleDevice? m_adapter;
    private IDisposable? m_subscriptionForObserver;
    private AdvGenerator? m_generator;

    public async Task StartScanAsync(IBleDevice adapter, Action<IGapAdvertisement> onNextAdvertisement)
    {
        StopScan();

        m_generator = new AdvGenerator();

        m_adapter = adapter;

        await adapter.InitializeAsync();
        m_observer = adapter.Observer;
        m_observer.Configure(
            new BleObservationParameters()
            {
                ScanType = ScanType.Active,
                ScanWindow = ScanTiming.Ms100,
                ScanInterval = ScanTiming.Ms100,
            }
        );
        await m_observer.StartObservingAsync();
        m_subscriptionForObserver = m_observer.OnAdvertisement(onNextAdvertisement);
    }

    public void StopScan()
    {
        _ = m_observer?.StopObservingAsync();

        m_subscriptionForObserver?.Dispose();
        m_subscriptionForObserver = null;

        if (m_observer is not null)
            _ = m_observer.StopObservingAsync();
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

    public async Task Initialize(IBleDevice bleDevice, MockDeviceSettings settings)
    {
        // Configure custom tx power to rssi behavior
        settings.TxPowerToRssi = txPower => (Rssi)((double)txPower / 3.0 * -2.0);

        IAdvertisingSet set = await bleDevice.Broadcaster.CreateAdvertisingSetAsync().ConfigureAwait(false);
        await set.StartAdvertisingAsync().ConfigureAwait(false);
        IObservable<AdvGenerator.DataExt> source = m_generator ?? Observable.Empty<AdvGenerator.DataExt>();
        source.Subscribe(x =>
        {
            set.SetRandomAddressAsync(x.Address).GetAwaiter().GetResult();
            set.SetAdvertisingParametersAsync(
                    new AdvertisingParameters
                    {
                        Type = BleEventType.None,
                        PrimaryPhy = Physical.Le1M,
                        AdvertisingSId = AdvertisingSId.NoAdIProvided,
                        AdvertisingTxPower = x.TxPower,
                        PeerAddress = BleAddress.NotAvailable,
                    }
                )
                .GetAwaiter()
                .GetResult();
            set.SetAdvertisingDataAsync(x.Data).GetAwaiter().GetResult();
        });
    }
}
