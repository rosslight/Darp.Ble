using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Services;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientService(
    BleUuid uuid,
    GattServiceType type,
    MockedBlePeripheral blePeripheral
) : GattClientService(blePeripheral, uuid, type)
{
    private ushort _handle;
    public MockedBlePeripheral BlePeripheral { get; } = blePeripheral;

    /// <inheritdoc />
    protected override Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<IGattClientCharacteristic>(
            new MockGattClientCharacteristic(this, _handle++, uuid, gattProperty, onRead, onWrite)
        );
    }
}
