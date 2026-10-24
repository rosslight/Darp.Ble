using System.Buffers.Binary;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary>
/// The GATT service a mock peripheral serves itself, one per link: Service Changed tells the central when services were
/// added, and Database Hash changes with the services
/// </summary>
internal sealed class MockGattService : IRemoteServiceDriver
{
    private readonly IReadOnlyList<IRemoteCharacteristicDriver> _characteristics;
    private readonly MockServiceChangedCharacteristic _serviceChanged;

    public MockGattService(MockLink link)
    {
        _serviceChanged = new MockServiceChangedCharacteristic(link);
        _characteristics = [_serviceChanged, new MockReadOnlyCharacteristic(0x2B2A, () => Hash(link.PeripheralDriver))];
    }

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x1801);

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult(_characteristics);

    /// <summary> The peripheral added a service </summary>
    public void OnServicesChanged() => _serviceChanged.Indicate();

    /// <summary>
    /// A hash of the published services: AES-CMAC with a zero key over their UUIDs, properties and descriptors. It
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

    /// <summary> Indicates the whole handle range while the central turned it on </summary>
    private sealed class MockServiceChangedCharacteristic(MockLink link) : IRemoteCharacteristicDriver
    {
        private readonly MockLink _link = link;
        private volatile bool _enabled;

        public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x2A05);

        public GattProperty Properties => GattProperty.Indicate;

        public void Indicate()
        {
            if (!_enabled)
                return;
            Span<byte> range = stackalloc byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(range, 0x0001);
            BinaryPrimitives.WriteUInt16LittleEndian(range[2..], 0xFFFF);
            _link.NotifyCentral(this, range);
        }

        public Task<byte[]> ReadAsync(GattRequest request) =>
            Task.FromException<byte[]>(
                new GattException(GattProtocolStatus.ReadNotPermitted, "Service Changed is only indicated")
            );

        public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
            Task.FromException(
                new GattException(GattProtocolStatus.WriteNotPermitted, "Service Changed is only indicated")
            );

        public ValueTask WriteWithoutResponseAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
            ValueTask.CompletedTask;

        public Task SetNotificationsAsync(bool enabled, GattRequest request)
        {
            request.OnTransactionSent();
            request.OnTransactionAnswered();
            _enabled = enabled;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request) =>
            Task.FromResult<IReadOnlyList<IRemoteDescriptorDriver>>([]);
    }
}
