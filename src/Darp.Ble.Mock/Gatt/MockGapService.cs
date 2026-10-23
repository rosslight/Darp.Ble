using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary> The GAP service a mock peripheral serves itself: the device name and appearance its app set </summary>
internal sealed class MockGapService : IRemoteServiceDriver
{
    private readonly IReadOnlyList<IRemoteCharacteristicDriver> _characteristics;

    public MockGapService(MockPeripheralDriver peripheral)
    {
        _characteristics =
        [
            new MockGapCharacteristic(0x2A00, () => System.Text.Encoding.UTF8.GetBytes(peripheral.Identity.DeviceName)),
            new MockGapCharacteristic(0x2A01, () => BitConverter.GetBytes((ushort)peripheral.Identity.Appearance)),
        ];
    }

    public BleUuid Uuid { get; } = BleUuid.FromUInt16(0x1800);

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult(_characteristics);
}
