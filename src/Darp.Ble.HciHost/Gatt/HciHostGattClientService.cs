using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt;

internal sealed class HciHostGattClientService(
    HciHostBlePeripheral peripheral,
    BleUuid uuid,
    GattServiceType type,
    ILogger<HciHostGattClientService> logger
) : GattClientService(peripheral, uuid, type, logger)
{
    protected override GattClientCharacteristic CreateCharacteristicCore(
        GattProperty properties,
        IGattCharacteristicValue value
    )
    {
        return new HciHostGattClientCharacteristic(
            this,
            properties,
            value,
            LoggerFactory.CreateLogger<HciHostGattClientCharacteristic>()
        );
    }
}
