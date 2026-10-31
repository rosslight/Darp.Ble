using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A service of a mock peripheral, as its central discovers it </summary>
internal sealed class MockRemoteService(GattServiceDefinition definition) : IRemoteServiceDriver
{
    private readonly GattServiceDefinition _definition = definition;

    public BleUuid Uuid => _definition.Uuid;

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteCharacteristicDriver>>([
            .. _definition.Characteristics.Select(characteristic => new MockRemoteCharacteristic(characteristic)),
        ]);
}
