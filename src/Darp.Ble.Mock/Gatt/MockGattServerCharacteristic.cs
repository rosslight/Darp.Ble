using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerCharacteristic(BleUuid uuid, GattClientCharacteristic characteristic) : IPlatformSpecificGattServerCharacteristic
{
    private readonly GattClientCharacteristic _characteristic = characteristic;
    public BleUuid Uuid { get; } = uuid;
    public Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {

    }
}