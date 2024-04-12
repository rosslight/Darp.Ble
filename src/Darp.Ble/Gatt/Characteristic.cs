using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

public sealed class Characteristic<TProp1>(BleUuid uuid)
{
    public BleUuid Uuid { get; } = uuid;
    public Characteristic(ushort uuid) : this(new BleUuid(uuid)) {}
}