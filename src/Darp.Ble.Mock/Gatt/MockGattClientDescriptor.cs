using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientDescriptor(
    MockGattClientCharacteristic clientCharacteristic,
    IGattCharacteristicValue value
) : GattClientDescriptor(clientCharacteristic, value);
