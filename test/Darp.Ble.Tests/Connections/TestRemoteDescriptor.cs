using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

/// <summary> A user description descriptor that keeps what is written to it and answers at once </summary>
internal sealed class TestRemoteDescriptor : IRemoteDescriptorDriver
{
    private byte[] _value = [];

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x2901);

    public Task<byte[]> ReadAsync(GattRequest request)
    {
        request.OnTransactionSent();
        request.OnTransactionAnswered();
        return Task.FromResult(_value);
    }

    public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        request.OnTransactionSent();
        _value = value.ToArray();
        request.OnTransactionAnswered();
        return Task.CompletedTask;
    }
}
