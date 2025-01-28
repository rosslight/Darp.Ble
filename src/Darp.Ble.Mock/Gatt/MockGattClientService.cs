using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientService(BleUuid uuid, MockedBlePeripheral blePeripheral) : GattClientService(blePeripheral, uuid)
{
    private ushort _handle;
    public MockedBlePeripheral BlePeripheral { get; } = blePeripheral;

    /// <inheritdoc />
    protected override Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid,
        GattProperty gattProperty,
        IGattClientService.OnReadCallback? onRead,
        IGattClientService.OnWriteCallback? onWrite,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IGattClientCharacteristic>(new MockGattClientCharacteristic(this, _handle++, uuid, gattProperty, onRead, onWrite));
    }
}