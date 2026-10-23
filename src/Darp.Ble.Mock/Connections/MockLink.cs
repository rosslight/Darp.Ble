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

    // The central's requests waiting for the peripheral's answer; the link going down fails them at once
    private readonly HashSet<TaskCompletionSource<object?>> _outstanding = [];
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
        Peripheral = new MockCentralConnection(this, centralAddress);
    }

    /// <summary> The peripheral role the central connected to </summary>
    public MockPeripheralDriver PeripheralDriver { get; }

    /// <summary> The GAP service the peripheral serves itself </summary>
    public MockGapService GapService { get; }

    /// <summary> The central's side </summary>
    public MockConnectionDriver Central { get; }

    /// <summary> The peripheral's side </summary>
    public MockCentralConnection Peripheral { get; }

    /// <summary> The central's object for a service; the same one on every discovery </summary>
    public MockRemoteService GetService(GattServiceDefinition definition)
    {
        lock (_lock)
        {
            if (!_services.TryGetValue(definition, out MockRemoteService? service))
            {
                service = new MockRemoteService(this, definition);
                _services.Add(definition, service);
            }
            return service;
        }
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

    /// <summary> The central's object for a descriptor; the same one on every discovery </summary>
    public MockRemoteDescriptor GetDescriptor(GattDescriptorDefinition definition)
    {
        lock (_lock)
        {
            if (!_descriptors.TryGetValue(definition, out MockRemoteDescriptor? descriptor))
            {
                descriptor = new MockRemoteDescriptor(this, definition);
                _descriptors.Add(definition, descriptor);
            }
            return descriptor;
        }
    }

    /// <summary> The central reads; the peripheral's handler answers </summary>
    public async Task<byte[]> ReadAsync(GattAttributeDefinition characteristic) =>
        (byte[]?)
            await CarryAsync(async () =>
                    // A transport copies what it sends; the central never shares the peripheral's buffer
                    (await _peripheralSink.OnReadRequest(Peripheral, characteristic).ConfigureAwait(false)).ToArray()
                )
                .ConfigureAwait(false)
        ?? [];

    /// <summary> The central writes with a write request; the peripheral's handler answers </summary>
    public Task WriteAsync(GattAttributeDefinition characteristic, ReadOnlyMemory<byte> value) =>
        CarryAsync(async () =>
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
        if (!TakeDown(out TaskCompletionSource<object?>[] outstanding))
            return;
        _peripheralSink.OnCentralDisconnected(Peripheral, new DisconnectInfo(DisconnectReason.RemoteRequest));
        Fail(outstanding);
    }

    public void DisconnectFromPeripheral()
    {
        if (!TakeDown(out TaskCompletionSource<object?>[] outstanding))
            return;
        // A driver reports the disconnect before it fails what still runs
        _centralSink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));
        Fail(outstanding);
    }

    /// <summary> Carries a request to the peripheral; its answer, or the link going down, ends it </summary>
    private Task<object?> CarryAsync(Func<Task<object?>> answer)
    {
        var outstanding = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            if (_down)
                throw new IOException("The mock link is down");
            _outstanding.Add(outstanding);
        }
        // The peripheral's handler may take longer than the link lives; its result is observed either way
        _ = answer()
            .ContinueWith(
                task =>
                {
                    lock (_lock)
                        _outstanding.Remove(outstanding);
                    if (task.IsCompletedSuccessfully)
                        outstanding.TrySetResult(task.Result);
                    else
                        outstanding.TrySetException(task.Exception?.InnerExceptions ?? [new IOException("Cancelled")]);
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

    private static void Fail(TaskCompletionSource<object?>[] outstanding)
    {
        foreach (TaskCompletionSource<object?> request in outstanding)
            request.TrySetException(new IOException("The mock link is down"));
    }

    private bool TakeDown(out TaskCompletionSource<object?>[] outstanding)
    {
        lock (_lock)
        {
            if (_down)
            {
                outstanding = [];
                return false;
            }
            _down = true;
            outstanding = [.. _outstanding];
            _outstanding.Clear();
        }
        PeripheralDriver.Forget(this);
        return true;
    }
}
