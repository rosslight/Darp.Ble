using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientService(
    BleUuid uuid,
    GattServiceType type,
    MockedBlePeripheral blePeripheral,
    ILogger<MockGattClientService> logger
) : GattClientService(blePeripheral, uuid, type, logger)
{
    public MockedBlePeripheral BlePeripheral { get; } = blePeripheral;

    /// <inheritdoc />
    protected override GattClientCharacteristic CreateCharacteristicCore(
        GattProperty properties,
        IGattCharacteristicValue value
    )
    {
        return new MockGattClientCharacteristic(
            this,
            properties,
            value,
            LoggerFactory.CreateLogger<MockGattClientCharacteristic>()
        );
    }
}
