using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Gatt;
using Darp.Ble.WinRT.Gatt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// This app's session with a peripheral. Ending it releases the session; Windows keeps the physical link while
/// another app uses the device
/// </summary>
internal sealed class WinRTConnectionDriver : IConnectionDriver
{
    private readonly BluetoothLEDevice _device;
    private readonly GattSession _session;
    private readonly IConnectionSink _sink;

    // One object per attribute handle, so every discovery returns the same ones and notifications find them
    private readonly AttributeWrappers<GattDeviceService, WinRTRemoteService> _services;
    private readonly AttributeWrappers<GattCharacteristic, WinRTRemoteCharacteristic> _characteristics;
    private readonly Dictionary<ushort, WinRTRemoteDescriptor> _descriptors = [];
    private bool _accessGranted;
    private int _ended;

    public WinRTConnectionDriver(BluetoothLEDevice device, GattSession session, IConnectionSink sink)
    {
        _device = device;
        _session = session;
        _sink = sink;
        // A service that arrives after the end is released; a characteristic holds nothing to release
        _services = new(s => s.AttributeHandle, s => new WinRTRemoteService(this, s), s => s.Dispose());
        _characteristics = new(c => c.AttributeHandle, c => new WinRTRemoteCharacteristic(this, c), _ => { });
        _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        _session.SessionStatusChanged += OnSessionStatusChanged;
        _session.MaxPduSizeChanged += OnMaxPduSizeChanged;
        // The link may have ended before the handlers were attached
        if (
            _device.ConnectionStatus is BluetoothConnectionStatus.Disconnected
            || _session.SessionStatus is GattSessionStatus.Closed
        )
            LinkDown(DisconnectReason.RemoteRequest);
    }

    public IConnectionSink Sink => _sink;

    /// <summary> Reports Windows' MTU; Windows exchanges it itself, often after this, and reports the change </summary>
    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) => Task.FromResult((int)_session.MaxPduSize);

    public async Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request)
    {
        await EnsureAccessAsync(request).ConfigureAwait(false);
        GattDeviceServicesResult result = await NativeCall
            .RunAsync(request, () => _device.GetGattServicesAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        ThrowIfFailed(result.Status, result.ProtocolError, "discover services");
        return _services.Wrap(result.Services) ?? throw Ended();
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

    /// <summary> Wraps discovered characteristics; their wrappers receive the notifications </summary>
    public IReadOnlyList<WinRTRemoteCharacteristic> WrapCharacteristics(
        IReadOnlyList<GattCharacteristic> characteristics
    ) => _characteristics.Wrap(characteristics) ?? throw Ended();

    /// <summary> A discovery Windows answered after this connection ended; the core reports the end instead </summary>
    private static BleException Ended() => new("The connection ended before Windows answered");

    /// <summary> Only discovery, one request at a time, asks for descriptors; disposal does not need them </summary>
    public WinRTRemoteDescriptor GetDescriptor(GattDescriptor descriptor)
    {
        if (_descriptors.TryGetValue(descriptor.AttributeHandle, out WinRTRemoteDescriptor? known))
            return known;
        var created = new WinRTRemoteDescriptor(this, descriptor);
        _descriptors.Add(descriptor.AttributeHandle, created);
        return created;
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus is BluetoothConnectionStatus.Disconnected)
            LinkDown(DisconnectReason.RemoteRequest);
    }

    private void OnMaxPduSizeChanged(GattSession sender, object args) => _sink.OnMtuChanged(sender.MaxPduSize);

    private void OnSessionStatusChanged(GattSession sender, GattSessionStatusChangedEventArgs args)
    {
        if (args.Status is GattSessionStatus.Closed)
            LinkDown(DisconnectReason.RemoteRequest);
    }

    /// <summary>
    /// Fails with the peer's ATT error, the lost link or Windows' refusal. A device Windows cannot reach ends the
    /// connection: it is reported first, so the core fails the request as a disconnect
    /// </summary>
    public void ThrowIfFailed(GattCommunicationStatus status, byte? protocolError, string action)
    {
        switch (status)
        {
            case GattCommunicationStatus.Success:
                return;
            case GattCommunicationStatus.ProtocolError:
                throw new GattException(
                    (GattProtocolStatus)(protocolError ?? (byte)GattProtocolStatus.UnlikelyError),
                    $"The peer answered {action} with {protocolError}"
                );
            case GattCommunicationStatus.Unreachable:
                LinkDown(DisconnectReason.ConnectionTimeout);
                throw new BleException($"Windows could not reach the device to {action}");
            default:
                // AccessDenied: Windows does not let this app use the device. No lost link, so the connection stays
                throw new BleException(
                    $"Windows refused to {action}: {status}",
                    isTransient: false,
                    nativeCode: (int)status,
                    innerException: null
                );
        }
    }

    /// <summary> Reports the end once. Windows' status events tell no reason; the peer or the link ended the connection </summary>
    private void LinkDown(DisconnectReason reason)
    {
        if (Interlocked.Exchange(ref _ended, 1) == 0)
            _sink.OnDisconnected(new DisconnectInfo(reason));
    }

    /// <summary> Releases this app's part without waiting for the physical link </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _ended, 1);
        _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _session.SessionStatusChanged -= OnSessionStatusChanged;
        _session.MaxPduSizeChanged -= OnMaxPduSizeChanged;
        foreach (WinRTRemoteCharacteristic characteristic in _characteristics.End())
            characteristic.Detach();
        foreach (WinRTRemoteService service in _services.End())
            service.Dispose();
        _session.MaintainConnection = false;
        _session.Dispose();
        _device.Dispose();
        return ValueTask.CompletedTask;
    }
}
