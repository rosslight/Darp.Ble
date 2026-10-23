using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A characteristic of a mock peripheral, as its central discovers it; requests reach the peripheral's handlers </summary>
internal sealed class MockRemoteCharacteristic(MockLink link, GattCharacteristicDefinition definition)
    : IRemoteCharacteristicDriver
{
    private readonly MockLink _link = link;
    private readonly GattCharacteristicDefinition _definition = definition;

    public BleUuid Uuid => _definition.Uuid;

    public GattProperty Properties => _definition.Properties;

    public async Task<byte[]> ReadAsync(GattRequest request)
    {
        request.OnTransactionSent();
        try
        {
            return await _link.ReadAsync(_definition).ConfigureAwait(false);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        request.OnTransactionSent();
        try
        {
            await _link.WriteAsync(_definition, value).ConfigureAwait(false);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
    }

    public Task SetNotificationsAsync(bool enabled, GattRequest request)
    {
        request.OnTransactionSent();
        try
        {
            _link.SetSubscribed(_definition, enabled);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
        return Task.CompletedTask;
    }

    /// <summary> Accepted at once; the peripheral's handler runs without the central waiting for it </summary>
    public ValueTask WriteWithoutResponseAsync(ReadOnlyMemory<byte> value, GattRequest request)
    {
        request.HandOffToken.ThrowIfCancellationRequested();
        _link.WriteWithoutResponse(_definition, value);
        return ValueTask.CompletedTask;
    }
}
