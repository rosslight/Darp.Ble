using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Examples.Unix.Mockup;

internal class BMBroadcaster : BleBroadcaster
{
    private BMAdvGenerator? m_generator;
    private AdvertisingParameters? m_parameters;
    private CancellationTokenSource? m_cancellationTokenSource;

    protected override IDisposable AdvertiseCore(IObservable<AdvertisingData> source, AdvertisingParameters? parameters)
    {
        m_cancellationTokenSource = new CancellationTokenSource();
        m_generator = new BMAdvGenerator();
        m_parameters = parameters;

        return Disposable.Create(this, state =>
        {
            state.m_generator?.Dispose();
            state.m_generator = null;
        });
    }

    protected override void StopAllCore()
    {
        m_cancellationTokenSource?.Cancel();
        m_cancellationTokenSource = null;

        m_generator?.Stop();
        m_generator = null;
    }

    public IObservable<IGapAdvertisement> GetAdvertisements(BleObserver observer)
    {
        IObservable<(BleAddress Address, AdvertisingData Data)> source = m_generator ?? Observable.Empty<(BleAddress Address, AdvertisingData Data)>();
        return source
            .TakeWhile(_ => m_cancellationTokenSource?.IsCancellationRequested != true)
            .Select(x => GapAdvertisement.FromExtendedAdvertisingReport(
                observer,
                DateTimeOffset.UtcNow,
                m_parameters?.Type ?? BleEventType.None,
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
}

