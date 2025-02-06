using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientDescriptor(
    MockGattClientCharacteristic clientCharacteristic,
    BleUuid uuid,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite
) : GattClientDescriptor(clientCharacteristic, uuid, onRead, onWrite);
