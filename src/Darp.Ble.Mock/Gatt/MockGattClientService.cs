using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Services;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientService(
    BleUuid uuid,
    GattServiceType type,
    MockedBlePeripheral blePeripheral,
    GattClientService? previousService,
    ILogger<MockGattClientService> logger
) : GattClientService(blePeripheral, uuid, type, previousService, logger)
{
    public MockedBlePeripheral BlePeripheral { get; } = blePeripheral;

    /// <inheritdoc />
    protected override Task<GattClientCharacteristic> CreateCharacteristicAsyncCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        GattClientCharacteristic? previousCharacteristic,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<GattClientCharacteristic>(
            new MockGattClientCharacteristic(
                this,
                uuid,
                gattProperty,
                onRead,
                onWrite,
                previousCharacteristic,
                LoggerFactory.CreateLogger<MockGattClientCharacteristic>()
            )
        );
    }
}
