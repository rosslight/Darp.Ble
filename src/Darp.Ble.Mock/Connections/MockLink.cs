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
    private readonly Dictionary<GattDescriptorDefinition, MockRemoteDescriptor> _descriptors = [];
    private readonly HashSet<GattCharacteristicDefinition> _subscribed = [];

    // Fails the central's request that waits for the peripheral's answer; one at a time, as the core holds the ATT slot
    // until it ends. The link going down fails it at once
    private Action? _failOutstanding;
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
        GapService = new MockGapService(peripheral);
        GattService = peripheral.IncludeGattService ? new MockGattService(this) : null;
        Peripheral = new MockCentralConnection(this, centralAddress);
    }

    /// <summary> The peripheral role the central connected to </summary>
    public MockPeripheralDriver PeripheralDriver { get; }

    /// <summary> The GAP service the peripheral serves itself </summary>
    public MockGapService GapService { get; }

    /// <summary> The GATT service the peripheral serves itself, or null for one without it </summary>
    public MockGattService? GattService { get; }

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

    /// <summary> The central's object for a descriptor; the same one on every discovery, which the core runs one at a time </summary>
    public MockRemoteDescriptor GetDescriptor(GattDescriptorDefinition definition)
    {
        if (!_descriptors.TryGetValue(definition, out MockRemoteDescriptor? descriptor))
        {
            descriptor = new MockRemoteDescriptor(this, definition);
            _descriptors.Add(definition, descriptor);
        }
        return descriptor;
    }

    /// <summary> The central reads; the peripheral's handler answers </summary>
    public Task<byte[]> ReadAsync(GattAttributeDefinition characteristic) =>
        CarryAsync(async () =>
            // A transport copies what it sends; the central never shares the peripheral's buffer
            (await _peripheralSink.OnReadRequest(Peripheral, characteristic).ConfigureAwait(false)).ToArray()
        );

    /// <summary> The central writes with a write request; the peripheral's handler answers </summary>
    public Task WriteAsync(GattAttributeDefinition characteristic, ReadOnlyMemory<byte> value) =>
        CarryAsync<object?>(async () =>
        {
            await _peripheralSink.OnWriteRequest(Peripheral, characteristic, value).ConfigureAwait(false);
            return null;
        });

    /// <summary> The central writes with a write command; the peripheral's handler runs on its own, its answer is dropped </summary>
    public void WriteWithoutResponse(GattCharacteristicDefinition characteristic, ReadOnlyMemory<byte> value)
    {
        EnsureUp();
        // The core copies the value before the handler runs, so the caller's buffer is free once this returns
        _peripheralSink.OnWriteCommand(Peripheral, characteristic, value);
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
        // As a peripheral's stack does when a CCCD is written
        _peripheralSink.OnSubscriptionChanged(Peripheral, characteristic, enabled);
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
        if (!TakeDown(out Action? failOutstanding))
            return;
        _peripheralSink.OnCentralDisconnected(Peripheral, new DisconnectInfo(DisconnectReason.RemoteRequest));
        failOutstanding?.Invoke();
    }

    public void DisconnectFromPeripheral()
    {
        if (!TakeDown(out Action? failOutstanding))
            return;
        // A driver reports the disconnect before it fails what still runs
        _centralSink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));
        failOutstanding?.Invoke();
    }

    /// <summary> Carries a request to the peripheral; its answer, or the link going down, ends it </summary>
    private Task<T> CarryAsync<T>(Func<Task<T>> answer)
    {
        var outstanding = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action fail = () => outstanding.TrySetException(new IOException("The mock link is down"));
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
            _failOutstanding = fail;
        }
        // The peripheral's handler may take longer than the link lives; its result is observed either way
        _ = answer()
            .ContinueWith(
                task =>
                {
                    lock (_lock)
                    {
                        if (_failOutstanding == fail)
                            _failOutstanding = null;
                    }
                    outstanding.TrySetFromTask(task);
                },
                TaskScheduler.Default
            );
        return outstanding.Task;
    }

    private void EnsureUp()
    {
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
        }
    }

    private bool TakeDown(out Action? failOutstanding)
    {
        lock (_lock)
        {
            if (_down)
            {
                failOutstanding = null;
                return false;
            }
            _down = true;
            failOutstanding = _failOutstanding;
            _failOutstanding = null;
        }
        PeripheralDriver.Forget(this);
        return true;
    }
}
