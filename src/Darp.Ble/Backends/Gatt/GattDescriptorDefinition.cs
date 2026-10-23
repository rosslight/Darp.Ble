using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A descriptor of a published characteristic. The object itself identifies it </summary>
public sealed class GattDescriptorDefinition : GattAttributeDefinition
{
    internal GattDescriptorDefinition(BleUuid uuid, ReadOnlyMemory<byte>? staticValue)
        : base(uuid)
    {
        StaticValue = staticValue;
    }

    /// <summary> The fixed, read-only value of a generated descriptor such as a user description; null when handlers answer </summary>
    /// <remarks> Backends that publish such descriptors themselves (WinRT) use it; others may also ask the core like for any descriptor </remarks>
    public ReadOnlyMemory<byte>? StaticValue { get; }
}
