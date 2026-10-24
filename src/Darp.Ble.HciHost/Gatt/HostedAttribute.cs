using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> One attribute of the database </summary>
internal sealed class HostedAttribute
{
    private readonly Func<byte[]>? _value;

    /// <summary> An attribute with a fixed value, e.g. a declaration </summary>
    public HostedAttribute(ushort handle, BleUuid type, byte[] value)
        : this(handle, type, () => value) { }

    /// <summary> An attribute whose value the backend knows, e.g. of the GAP service </summary>
    public HostedAttribute(ushort handle, BleUuid type, Func<byte[]> value)
    {
        Handle = handle;
        Type = type;
        _value = value;
    }

    /// <summary> A characteristic value or descriptor the app answers for, or a CCCD of a characteristic </summary>
    public HostedAttribute(
        ushort handle,
        BleUuid type,
        GattAttributeDefinition? hosted,
        GattCharacteristicDefinition? cccdOf
    )
    {
        Handle = handle;
        Type = type;
        Hosted = hosted;
        CccdOf = cccdOf;
    }

    public ushort Handle { get; }

    public BleUuid Type { get; }

    /// <summary> The last handle of a service, for its declaration </summary>
    public ushort GroupEnd { get; set; }

    /// <summary> The app's characteristic or descriptor; the core answers for it </summary>
    public GattAttributeDefinition? Hosted { get; }

    /// <summary> The characteristic this CCCD configures </summary>
    public GattCharacteristicDefinition? CccdOf { get; }

    /// <summary> Whether this is the CCCD of the GATT service's Service Changed characteristic, which the backend serves </summary>
    public bool IsServiceChangedCccd { get; init; }

    /// <summary> The value of a fixed or backend attribute; null when someone else answers </summary>
    public byte[]? StaticValue => _value?.Invoke();
}
