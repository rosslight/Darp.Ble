using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A characteristic of an added service. The object itself identifies it, also when two have the same UUID </summary>
public sealed class GattCharacteristicDefinition
{
    internal GattCharacteristicDefinition(BleUuid uuid, GattProperty properties)
    {
        Uuid = uuid;
        Properties = properties;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties { get; }
}
