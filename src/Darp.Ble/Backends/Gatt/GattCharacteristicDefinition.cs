using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A characteristic of an added service. The object itself identifies it, also when two have the same UUID </summary>
/// <remarks> The backend adds the client characteristic configuration descriptor of a notifying characteristic itself </remarks>
[Experimental(Experiments.Backends)]
public sealed class GattCharacteristicDefinition : GattAttributeDefinition
{
    internal GattCharacteristicDefinition(BleUuid uuid, GattProperty properties)
        : base(uuid)
    {
        Properties = properties;
    }

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties { get; }

    /// <summary> The descriptors the app hosts, in the order they were added; fixed once the service is added </summary>
    public IReadOnlyList<GattDescriptorDefinition> Descriptors { get; internal set; } = [];
}
