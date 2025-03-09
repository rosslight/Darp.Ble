using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientDescriptor(
    MockGattClientCharacteristic clientCharacteristic,
    IGattCharacteristicValue value
) : GattClientDescriptor(clientCharacteristic, value);
