using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A service of a mock peripheral, as its central discovers it </summary>
internal sealed class MockRemoteService(MockLink link, GattServiceDefinition definition) : IRemoteServiceDriver
{
    private readonly MockLink _link = link;
    private readonly GattServiceDefinition _definition = definition;

    public BleUuid Uuid => _definition.Uuid;

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteCharacteristicDriver>>([
            .. _definition.Characteristics.Select(_link.GetCharacteristic),
        ]);
}
