using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.WinRT.Gatt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// This app's session with a peripheral. Ending it releases the session; Windows keeps the physical link while
/// another app uses the device (D11)
/// </summary>
internal sealed class WinRTConnectionDriver : IConnectionDriver
{
    private readonly BluetoothLEDevice _device;
    private readonly GattSession _session;
    private readonly IConnectionSink _sink;
    private readonly Lock _lock = new();

    // One object per attribute handle, so every discovery returns the same ones and notifications find them
    private readonly Dictionary<ushort, WinRTRemoteService> _services = [];
    private readonly Dictionary<ushort, WinRTRemoteCharacteristic> _characteristics = [];
    private readonly Dictionary<ushort, WinRTRemoteDescriptor> _descriptors = [];
    private bool _accessGranted;
    private int _ended;

    public WinRTConnectionDriver(BluetoothLEDevice device, GattSession session, IConnectionSink sink)
    {
        _device = device;
        _session = session;
        _sink = sink;
        _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        _session.SessionStatusChanged += OnSessionStatusChanged;
        // The link may have ended before the handlers were attached
        if (
            _device.ConnectionStatus is BluetoothConnectionStatus.Disconnected
            || _session.SessionStatus is GattSessionStatus.Closed
        )
            LinkDown();
    }

    public IConnectionSink Sink => _sink;

    /// <summary> Reports Windows' MTU; Windows exchanges it itself and may change it later </summary>
    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) => Task.FromResult((int)_session.MaxPduSize);

    public async Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request)
    {
        await EnsureAccessAsync(request).ConfigureAwait(false);
        GattDeviceServicesResult result = await NativeCall
            .RunAsync(request, () => _device.GetGattServicesAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "discover services");
        List<IRemoteServiceDriver> services = [];
        foreach (GattDeviceService service in result.Services)
            services.Add(GetService(service));
        return services;
    }

    /// <summary> Windows asks the user once whether the app may use the device </summary>
    private async Task EnsureAccessAsync(GattRequest request)
    {
        if (_accessGranted)
            return;
        // Nothing is sent yet, so the wait for the user's answer ends with the request
        DeviceAccessStatus status = await _device
            .RequestAccessAsync()
            .AsTask(request.HandOffToken)
            .ConfigureAwait(false);
        if (status is not DeviceAccessStatus.Allowed)
            throw new BleException($"Windows does not allow access to the device: {status}");
        _accessGranted = true;
    }

    private WinRTRemoteService GetService(GattDeviceService service)
    {
        lock (_lock)
        {
            if (_services.TryGetValue(service.AttributeHandle, out WinRTRemoteService? known))
            {
                // A rediscovered service is a new native object for the same attribute
                service.Dispose();
                return known;
            }
            var created = new WinRTRemoteService(this, service);
            _services.Add(service.AttributeHandle, created);
            return created;
        }
    }

    public WinRTRemoteCharacteristic GetCharacteristic(GattCharacteristic characteristic)
    {
        lock (_lock)
        {
            if (_characteristics.TryGetValue(characteristic.AttributeHandle, out WinRTRemoteCharacteristic? known))
                return known;
            var created = new WinRTRemoteCharacteristic(this, characteristic);
            _characteristics.Add(characteristic.AttributeHandle, created);
            return created;
        }
    }

    public WinRTRemoteDescriptor GetDescriptor(GattDescriptor descriptor)
    {
        lock (_lock)
        {
            if (_descriptors.TryGetValue(descriptor.AttributeHandle, out WinRTRemoteDescriptor? known))
                return known;
            var created = new WinRTRemoteDescriptor(descriptor);
            _descriptors.Add(descriptor.AttributeHandle, created);
            return created;
        }
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus is BluetoothConnectionStatus.Disconnected)
            LinkDown();
    }

    private void OnSessionStatusChanged(GattSession sender, GattSessionStatusChangedEventArgs args)
    {
        if (args.Status is GattSessionStatus.Closed)
            LinkDown();
    }

    /// <summary> Windows tells no reason; the peer or the link ended the connection </summary>
    private void LinkDown()
    {
        if (Interlocked.Exchange(ref _ended, 1) == 0)
            _sink.OnDisconnected(new DisconnectInfo(DisconnectReason.RemoteRequest));
    }

    /// <summary> Releases this app's part without waiting for the physical link </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _ended, 1);
        _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _session.SessionStatusChanged -= OnSessionStatusChanged;
        WinRTRemoteService[] services;
        WinRTRemoteCharacteristic[] characteristics;
        lock (_lock)
        {
            services = [.. _services.Values];
            characteristics = [.. _characteristics.Values];
        }
        foreach (WinRTRemoteCharacteristic characteristic in characteristics)
            characteristic.Detach();
        foreach (WinRTRemoteService service in services)
            service.Dispose();
        _session.MaintainConnection = false;
        _session.Dispose();
        _device.Dispose();
        return ValueTask.CompletedTask;
    }
}
