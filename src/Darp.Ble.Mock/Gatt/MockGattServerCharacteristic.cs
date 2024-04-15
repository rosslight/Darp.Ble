using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerCharacteristic(BleUuid uuid, MockGattClientCharacteristic characteristic)
    : GattServerCharacteristic(uuid)
{
    private readonly MockGattClientCharacteristic _characteristic = characteristic;

    protected override Task WriteInternalAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        return _characteristic.WriteAsync(null!, bytes, cancellationToken);
    }

    public async Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}