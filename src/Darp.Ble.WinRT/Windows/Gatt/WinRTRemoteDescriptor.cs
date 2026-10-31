using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.WinRT.Connections;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinRTRemoteDescriptor(WinRTConnectionDriver connection, GattDescriptor descriptor)
    : IRemoteDescriptorDriver
{
    private readonly WinRTConnectionDriver _connection = connection;
    private readonly GattDescriptor _descriptor = descriptor;

    public BleUuid Uuid { get; } = BleUuid.FromGuid(descriptor.Uuid, inferType: true);

    public async Task<byte[]> ReadAsync(GattRequest request)
    {
        GattReadResult result = await NativeCall
            .RunAsync(request, () => _descriptor.ReadValueAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        _connection.ThrowIfFailed(result.Status, result.ProtocolError, "read the descriptor");
        return result.Value.ToArray();
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        GattWriteResult result = await NativeCall
            .RunAsync(request, () => _descriptor.WriteValueWithResultAsync(value.ToArray().AsBuffer()))
            .ConfigureAwait(false);
        _connection.ThrowIfFailed(result.Status, result.ProtocolError, "write the descriptor");
    }
}
