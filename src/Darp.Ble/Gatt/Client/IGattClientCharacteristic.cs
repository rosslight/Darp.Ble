using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientCharacteristic
{
    BleUuid Uuid { get; }
    GattProperty Property { get; }
    IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback);
    Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken);
}
public interface IGattClientCharacteristic<TProperty1>
{
    public BleUuid Uuid => Characteristic.Uuid;
    IGattClientCharacteristic Characteristic { get; }
}