using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A descriptor of a mock peripheral, as its central discovers it; requests reach the peripheral's handlers </summary>
internal sealed class MockRemoteDescriptor(MockLink link, GattDescriptorDefinition definition) : IRemoteDescriptorDriver
{
    private readonly MockLink _link = link;
    private readonly GattDescriptorDefinition _definition = definition;

    public BleUuid Uuid => _definition.Uuid;

    public async Task<byte[]> ReadAsync(GattRequest request)
    {
        // At the peripheral before later operations may go, so they reach its handlers after this one
        Task<byte[]> reading = _link.ReadAsync(_definition);
        request.OnTransactionSent();
        try
        {
            return await reading.ConfigureAwait(false);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        // At the peripheral before later operations may go, so they reach its handlers after this one
        Task writing = _link.WriteAsync(_definition, value);
        request.OnTransactionSent();
        try
        {
            await writing.ConfigureAwait(false);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
    }
}
