using Darp.Ble.Gatt.Client;

namespace Darp.Ble.HciHost.Gatt;

internal sealed class HciHostGattClientDescriptor(
    HciHostGattClientCharacteristic clientCharacteristic,
    IGattCharacteristicValue value
) : GattClientDescriptor(clientCharacteristic, value);
