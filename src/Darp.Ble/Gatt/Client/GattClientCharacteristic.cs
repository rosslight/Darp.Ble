using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Client;

public sealed class GattClientCharacteristic(BleUuid uuid, IPlatformSpecificGattClientCharacteristic characteristic)
{
    private readonly IPlatformSpecificGattClientCharacteristic _characteristic = characteristic;

    public BleUuid Uuid { get; } = uuid;
    public GattProperty Property => _characteristic.Property;

    public IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return _characteristic.OnWrite(callback);
    }

    public async Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken = default)
    {
        return await _characteristic.NotifyAsync(clientPeer, source, cancellationToken);
    }
}

public sealed class GattClientCharacteristic<TProp1>(GattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    public GattClientCharacteristic Characteristic { get; } = characteristic;
}