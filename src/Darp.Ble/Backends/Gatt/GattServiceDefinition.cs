using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A complete service to add; it does not change afterwards </summary>
public sealed class GattServiceDefinition
{
    internal GattServiceDefinition(
        BleUuid uuid,
        bool isPrimary,
        IReadOnlyList<GattCharacteristicDefinition> characteristics
    )
    {
        Uuid = uuid;
        IsPrimary = isPrimary;
        Characteristics = characteristics;
    }

    /// <summary> The UUID of the service </summary>
    public BleUuid Uuid { get; }

    /// <summary> Whether the service is primary; otherwise it is secondary </summary>
    public bool IsPrimary { get; }

    /// <summary> The characteristics, in the order they were added </summary>
    public IReadOnlyList<GattCharacteristicDefinition> Characteristics { get; }
}
