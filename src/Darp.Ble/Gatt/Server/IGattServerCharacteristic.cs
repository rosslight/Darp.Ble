using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerCharacteristic<TProp>
{
    BleUuid Uuid { get; }
    GattServerCharacteristic Characteristic { get; }
}

public sealed class GattServerCharacteristic<TProp1>(GattServerCharacteristic serverCharacteristic) : IGattServerCharacteristic<TProp1>
{
    public BleUuid Uuid => Characteristic.Uuid;
    public GattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}