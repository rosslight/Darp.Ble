using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

public sealed class Characteristic<TProp1>(BleUuid uuid) where TProp1 : IBleProperty
{
    public BleUuid Uuid { get; } = uuid;
    public GattProperty Property => TProp1.GattProperty;
    public Characteristic(ushort uuid) : this(new BleUuid(uuid)) {}
}