using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A hosted attribute centrals read or write: a characteristic or a descriptor. The object itself identifies it </summary>
public abstract class GattAttributeDefinition
{
    private protected GattAttributeDefinition(BleUuid uuid)
    {
        Uuid = uuid;
    }

    /// <summary> The UUID of the attribute </summary>
    public BleUuid Uuid { get; }
}
