using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic the adapter hosts, as added to a <see cref="LocalGattServiceBuilder"/> </summary>
public sealed class LocalGattCharacteristic
{
    internal LocalGattCharacteristic(GattCharacteristicDefinition definition)
    {
        Definition = definition;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid => Definition.Uuid;

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties => Definition.Properties;

    internal GattCharacteristicDefinition Definition { get; }
}
