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
    private readonly List<MockAdvertisingSetDriver> _advertising = [];

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

    public void StartAdvertising(MockAdvertisingSetDriver set)
    {
        lock (_lock)
            _advertising.Add(set);
    }

    public void StopAdvertising(MockAdvertisingSetDriver set)
    {
        lock (_lock)
            _advertising.Remove(set);
    }

    /// <summary> Finds a set that advertises connectable with this address, from another adapter than the central's </summary>
    public MockAdvertisingSetDriver? FindConnectable(MockAdapterDriver central, BleAddress address)
    {
        lock (_lock)
        {
            return _advertising.Find(set =>
                !ReferenceEquals(set.Adapter, central)
                && set.RandomAddress == address
                && set.Type.HasFlag(BleEventType.Connectable)
            );
        }
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
