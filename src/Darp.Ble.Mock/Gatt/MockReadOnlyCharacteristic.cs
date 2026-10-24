using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A read-only characteristic of a service the mock peripheral serves itself, e.g. GAP </summary>
internal sealed class MockReadOnlyCharacteristic(ushort uuid, Func<byte[]> read) : IRemoteCharacteristicDriver
{
    private readonly Func<byte[]> _read = read;

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(uuid);

    public GattProperty Properties => GattProperty.Read;

    public Task<byte[]> ReadAsync(GattRequest request)
    {
        request.OnTransactionSent();
        request.OnTransactionAnswered();
        return Task.FromResult(_read());
    }

    public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
        Task.FromException(new GattException(GattProtocolStatus.WriteNotPermitted, "The characteristic is read-only"));

    public ValueTask WriteWithoutResponseAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
        ValueTask.CompletedTask;

    public Task SetNotificationsAsync(bool enabled, GattRequest request) =>
        Task.FromException(
            new GattException(GattProtocolStatus.RequestNotSupported, "The characteristic does not notify")
        );

    public Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteDescriptorDriver>>([]);
}
