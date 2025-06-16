using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The descriptor declaration </summary>
public sealed class DescriptorDeclaration(BleUuid uuid) : IGattDeclaration
{
    /// <summary> The Characteristic Extended Properties declaration is a descriptor that defines additional Characteristic Properties </summary>
    public static DescriptorDeclaration CharacteristicExtendedProperties { get; } = new(0x2900);

    /// <summary> The Characteristic User Description declaration is an optional characteristic descriptor that defines a UTF-8 string of variable size that is a user textual description of the Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicUserDescription { get; } = new(0x2901);

    /// <summary> The Client Characteristic Configuration declaration is an optional characteristic descriptor that defines how the characteristic may be configured by a specific client </summary>
    public static DescriptorDeclaration ClientCharacteristicConfiguration { get; } = new(0x2902);

    /// <summary> The Server Characteristic Configuration declaration is an optional characteristic descriptor that defines how the characteristic may be configured for the server </summary>
    public static DescriptorDeclaration ServerCharacteristicConfiguration { get; } = new(0x2903);

    /// <summary> The Characteristic Presentation Format declaration is an optional characteristic descriptor that defines the format of the Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicPresentationFormat { get; } = new(0x2904);

    /// <summary> The Characteristic Aggregate Format declaration is an optional characteristic descriptor that defines the format of an aggregated Characteristic Value </summary>
    public static DescriptorDeclaration CharacteristicAggregateFormat { get; } = new(0x2905);

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
}
