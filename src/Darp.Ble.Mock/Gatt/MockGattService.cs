using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary>
/// The GATT service a mock peripheral serves itself, one per link. Database Hash changes with the services; Service
/// Changed is declared, but the core's client cannot turn indications on, so the mock sends none
/// </summary>
internal sealed class MockGattService : IRemoteServiceDriver
{
    private readonly IReadOnlyList<IRemoteCharacteristicDriver> _characteristics;

    public MockGattService(MockLink link)
    {
        _characteristics =
        [
            new MockServiceChangedCharacteristic(),
            new MockReadOnlyCharacteristic(0x2B2A, () => Hash(link.PeripheralDriver)),
        ];
    }

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x1801);

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult(_characteristics);

    /// <summary>
    /// A hash of the added services: AES-CMAC with a zero key over their UUIDs, properties and descriptors. It
    /// changes with the services as a peripheral's does; the mock has no handles, so the value differs from a real one
    /// </summary>
    private static byte[] Hash(MockPeripheralDriver peripheral)
    {
        List<byte> message = [];
        foreach (GattServiceDefinition service in peripheral.Services)
        {
            message.AddRange(service.Uuid.ToByteArray());
            message.Add(service.IsPrimary ? (byte)1 : (byte)0);
            foreach (GattCharacteristicDefinition characteristic in service.Characteristics)
            {
                message.AddRange(characteristic.Uuid.ToByteArray());
                message.Add((byte)characteristic.Properties);
                foreach (GattDescriptorDefinition descriptor in characteristic.Descriptors)
                    message.AddRange(descriptor.Uuid.ToByteArray());
            }
        }
        using var cmac = new AesCmac(new byte[16]);
        byte[] hash = cmac.Encrypt(message.ToArray());
        Array.Reverse(hash);
        return hash;
    }

    /// <summary> Service Changed: only indicated, which the core's client cannot turn on </summary>
    private sealed class MockServiceChangedCharacteristic : IRemoteCharacteristicDriver
    {
        public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x2A05);

        public GattProperty Properties => GattProperty.Indicate;

        public Task<byte[]> ReadAsync(GattRequest request) =>
            Task.FromException<byte[]>(
                new GattException(GattProtocolStatus.ReadNotPermitted, "Service Changed is only indicated")
            );

        public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
            Task.FromException(
                new GattException(GattProtocolStatus.WriteNotPermitted, "Service Changed is only indicated")
            );

        public ValueTask WriteWithoutResponseAsync(
            ReadOnlyMemory<byte> value,
            CancellationToken cancellationToken,
            CancellationToken connectionClosed
        ) => ValueTask.CompletedTask;

        public Task SetNotificationsAsync(bool enabled, GattRequest request) =>
            Task.FromException(new NotSupportedException("The mock sends no indications"));

        public Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request) =>
            Task.FromResult<IReadOnlyList<IRemoteDescriptorDriver>>([]);
    }
}
