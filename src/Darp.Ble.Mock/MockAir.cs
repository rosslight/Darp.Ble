using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Mock.Gap;

namespace Darp.Ble.Mock;

/// <summary> The radio space of one opened mock adapter and its peripherals: advertising reaches every other adapter that scans </summary>
internal sealed class MockAir(TimeProvider timeProvider, Func<TxPowerLevel, Rssi> txPowerToRssi)
{
    private readonly Func<TxPowerLevel, Rssi> _txPowerToRssi = txPowerToRssi;
    private readonly Lock _lock = new();
    private MockObserverDriver[] _listeners = [];

    public TimeProvider TimeProvider { get; } = timeProvider;

    public void StartListening(MockObserverDriver observer)
    {
        lock (_lock)
            _listeners = [.. _listeners, observer];
    }

    public void StopListening(MockObserverDriver observer)
    {
        lock (_lock)
            _listeners = [.. _listeners.Where(listener => !ReferenceEquals(listener, observer))];
    }

    /// <summary> Delivers one advertising event of a set to every listening adapter except the sender </summary>
    public void Transmit(MockAdapterDriver sender, MockAdvertisingState advertising)
    {
        MockObserverDriver[] listeners;
        lock (_lock)
            listeners = _listeners;
        DateTimeOffset now = TimeProvider.GetUtcNow();
        Rssi rssi = _txPowerToRssi(advertising.Parameters.AdvertisingTxPower);
        foreach (MockObserverDriver listener in listeners)
        {
            if (!ReferenceEquals(listener.Adapter, sender))
                listener.Receive(advertising, now, rssi);
        }
    }
}
