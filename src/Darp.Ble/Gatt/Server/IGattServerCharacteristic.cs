using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerCharacteristic
{
    BleUuid Uuid { get; }
    Task WriteAsync(byte[] bytes, CancellationToken cancellationToken = default);
    IObservable<byte[]> OnNotify();

}

public interface IGattServerCharacteristic<TProp>
{
    BleUuid Uuid { get; }
    IGattServerCharacteristic Characteristic { get; }
}

public sealed class GattServerCharacteristic<TProp1>(IGattServerCharacteristic serverCharacteristic) : IGattServerCharacteristic<TProp1>
{
    public BleUuid Uuid => Characteristic.Uuid;
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}