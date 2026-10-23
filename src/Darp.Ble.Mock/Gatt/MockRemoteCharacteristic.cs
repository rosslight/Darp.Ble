using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Mock.Gatt;

/// <summary> A characteristic of a mock peripheral, as its central discovers it </summary>
internal sealed class MockRemoteCharacteristic(GattCharacteristicDefinition definition) : IRemoteCharacteristicDriver
{
    private readonly GattCharacteristicDefinition _definition = definition;

    public BleUuid Uuid => _definition.Uuid;

    public GattProperty Properties => _definition.Properties;
}
