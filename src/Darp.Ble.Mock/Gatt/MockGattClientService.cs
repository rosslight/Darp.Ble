using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientService(BleUuid uuid, MockBlePeripheral blePeripheral) : GattClientService(uuid)
{
    public MockBlePeripheral BlePeripheral { get; } = blePeripheral;

    /// <inheritdoc />
    protected override Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid,
        GattProperty gattProperty,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IGattClientCharacteristic>(new MockGattClientCharacteristic(uuid, gattProperty, this));
    }
}