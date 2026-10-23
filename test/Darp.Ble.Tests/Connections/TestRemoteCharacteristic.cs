using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Tests.Connections;

/// <summary> A characteristic whose reads and writes without response wait until the test answers or accepts them </summary>
internal sealed class TestRemoteCharacteristic(TestConnectionDriver connection) : IRemoteCharacteristicDriver
{
    private readonly TestConnectionDriver _connection = connection;
    private readonly TestRemoteDescriptor _description = new();

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x2A19);

    public GattProperty Properties => GattProperty.Read | GattProperty.WriteWithoutResponse;

    public async Task<byte[]> ReadAsync(GattRequest request) =>
        (byte[]?)await _connection.HandOver("read", request).Done.ConfigureAwait(false) ?? [];

    public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
        throw new NotSupportedException("Not used by the tests");

    public Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteDescriptorDriver>>([_description]);

    public Task SetNotificationsAsync(bool enabled, GattRequest request) =>
        throw new NotSupportedException("Not used by the tests");

    /// <summary> Waits for buffer space until the test accepts it, honouring the hand-off token meanwhile </summary>
    public ValueTask WriteWithoutResponseAsync(
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken,
        CancellationToken connectionClosed
    )
    {
        PendingRequest pending = _connection.HandOverCommand();
        cancellationToken.Register(() => pending.Fail(new OperationCanceledException(cancellationToken)));
        return new ValueTask(pending.Done);
    }
}
