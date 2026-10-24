using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.WinRT.Connections;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinRTRemoteCharacteristic : IRemoteCharacteristicDriver
{
    private readonly WinRTConnectionDriver _connection;
    private readonly GattCharacteristic _characteristic;

    public WinRTRemoteCharacteristic(WinRTConnectionDriver connection, GattCharacteristic characteristic)
    {
        _connection = connection;
        _characteristic = characteristic;
        Uuid = BleUuid.FromGuid(characteristic.Uuid, inferType: true);
        Properties = WinRTMapping.ToProperties(characteristic.CharacteristicProperties);
        // Before any CCCD write, so a value the peer sends right after enabling is not lost
        _characteristic.ValueChanged += OnValueChanged;
    }

    public BleUuid Uuid { get; }

    public GattProperty Properties { get; }

    public async Task<byte[]> ReadAsync(GattRequest request)
    {
        GattReadResult result = await NativeCall
            .RunAsync(request, () => _characteristic.ReadValueAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "read");
        return result.Value.ToArray();
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        GattWriteResult result = await NativeCall
            .RunAsync(
                request,
                () =>
                    _characteristic.WriteValueWithResultAsync(
                        value.ToArray().AsBuffer(),
                        GattWriteOption.WriteWithResponse
                    )
            )
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "write");
    }

    /// <summary>
    /// Windows has no signal for buffer space: the write is handed off at once, and only waiting in the core's queue
    /// can be cancelled (D8). The task completes with Windows' result; the bytes are copied before the call
    /// </summary>
    public async ValueTask WriteWithoutResponseAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        request.HandOffToken.ThrowIfCancellationRequested();
        GattWriteResult result = await _characteristic
            .WriteValueWithResultAsync(value.ToArray().AsBuffer(), GattWriteOption.WriteWithoutResponse)
            .AsTask()
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "write without response");
    }

    public async Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request)
    {
        GattDescriptorsResult result = await NativeCall
            .RunAsync(request, () => _characteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "discover descriptors");
        return [.. result.Descriptors.Select(_connection.GetDescriptor)];
    }

    public async Task SetNotificationsAsync(bool enabled, GattRequest request)
    {
        GattWriteResult result = await NativeCall
            .RunAsync(
                request,
                () =>
                    _characteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(
                        enabled
                            ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                            : GattClientCharacteristicConfigurationDescriptorValue.None
                    )
            )
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "write the CCCD");
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args) =>
        _connection.Sink.OnNotification(this, args.CharacteristicValue.ToArray());

    public void Detach() => _characteristic.ValueChanged -= OnValueChanged;
}
