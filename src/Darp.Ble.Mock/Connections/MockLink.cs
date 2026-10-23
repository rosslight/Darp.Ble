using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock.Connections;

/// <summary> One connection between two mock adapters. It goes down once; the other side learns it as a remote disconnect </summary>
internal sealed class MockLink
{
    private readonly IConnectionSink _centralSink;
    private readonly IGattServerSink _peripheralSink;
    private readonly Lock _lock = new();
    private readonly Dictionary<GattServiceDefinition, MockRemoteService> _services = [];
    private readonly Dictionary<GattCharacteristicDefinition, MockRemoteCharacteristic> _characteristics = [];
    private readonly HashSet<GattCharacteristicDefinition> _subscribed = [];
    private bool _down;

    public MockLink(
        BleAddress centralAddress,
        IConnectionSink centralSink,
        MockPeripheralDriver peripheral,
        IGattServerSink peripheralSink
    )
    {
        PeripheralDriver = peripheral;
        _centralSink = centralSink;
        _peripheralSink = peripheralSink;
        Central = new MockConnectionDriver(this);
        Peripheral = new MockCentralConnection(this, centralAddress);
    }

    /// <summary> The peripheral role the central connected to </summary>
    public MockPeripheralDriver PeripheralDriver { get; }

    /// <summary> The central's side </summary>
    public MockConnectionDriver Central { get; }

    /// <summary> The peripheral's side </summary>
    public MockCentralConnection Peripheral { get; }

    /// <summary> The central's object for a service; the same one on every discovery, which the core runs one at a time </summary>
    public MockRemoteService GetService(GattServiceDefinition definition)
    {
        if (!_services.TryGetValue(definition, out MockRemoteService? service))
        {
            service = new MockRemoteService(this, definition);
            _services.Add(definition, service);
        }
        return service;
    }

    /// <summary> The central's object for a characteristic; the same one on every discovery </summary>
    public MockRemoteCharacteristic GetCharacteristic(GattCharacteristicDefinition definition)
    {
        lock (_lock)
        {
            if (!_characteristics.TryGetValue(definition, out MockRemoteCharacteristic? characteristic))
            {
                characteristic = new MockRemoteCharacteristic(this, definition);
                _characteristics.Add(definition, characteristic);
            }
            return characteristic;
        }
    }

    /// <summary> The central reads; the peripheral's handler answers </summary>
    public Task<byte[]> ReadAsync(GattCharacteristicDefinition characteristic)
    {
        EnsureUp();
        return _peripheralSink.OnReadRequest(Peripheral, characteristic);
    }

    /// <summary> The central writes with a write request; the peripheral's handler answers </summary>
    public Task WriteAsync(GattCharacteristicDefinition characteristic, ReadOnlyMemory<byte> value)
    {
        EnsureUp();
        return _peripheralSink.OnWriteRequest(Peripheral, characteristic, value);
    }

    /// <summary> The central writes the CCCD of a characteristic </summary>
    public void SetSubscribed(GattCharacteristicDefinition characteristic, bool enabled)
    {
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
            if (enabled)
                _subscribed.Add(characteristic);
            else
                _subscribed.Remove(characteristic);
        }
    }

    /// <summary> The peripheral notifies; the central gets it while it subscribed, as a peripheral's stack decides </summary>
    public void Notify(GattCharacteristicDefinition characteristic, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
            if (!_subscribed.Contains(characteristic))
                return;
        }
        _centralSink.OnNotification(GetCharacteristic(characteristic), value);
    }

    public void DisconnectFromCentral()
    {
        if (TakeDown())
            _peripheralSink.OnCentralDisconnected(Peripheral, new DisconnectInfo(DisconnectReason.RemoteRequest));
    }

    public void DisconnectFromPeripheral()
    {
        if (TakeDown())
            _centralSink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));
    }

    private void EnsureUp()
    {
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
        }
    }

    private bool TakeDown()
    {
        lock (_lock)
        {
            if (_down)
                return false;
            _down = true;
        }
        PeripheralDriver.Forget(this);
        return true;
    }
}
