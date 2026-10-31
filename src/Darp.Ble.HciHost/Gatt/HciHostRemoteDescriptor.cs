using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.HciHost.Connections;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> A descriptor of the peer </summary>
internal sealed class HciHostRemoteDescriptor(HciHostConnectionDriver connection, ushort handle, BleUuid uuid)
    : IRemoteDescriptorDriver
{
    private readonly HciHostConnectionDriver _connection = connection;

    public ushort Handle { get; } = handle;

    public BleUuid Uuid { get; } = uuid;

    public Task<byte[]> ReadAsync(GattRequest request) => _connection.ReadAsync(Handle, request);

    public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
        _connection.WriteAsync(Handle, value, request);
}
