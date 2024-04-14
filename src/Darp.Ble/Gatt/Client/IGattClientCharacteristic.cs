using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientCharacteristic<TProperty1>
{
    public BleUuid Uuid => Characteristic.Uuid;
    GattClientCharacteristic Characteristic { get; }
}