using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The gatt service declaration </summary>
public interface IGattServiceDeclaration : IGattAttributeDeclaration
{
    /// <summary> True, if service is a primary service; False, if service is a secondary service </summary>
    GattServiceType Type { get; }
}

/// <summary> A service declaration </summary>
/// <param name="uuid"> The uuid of the declared service </param>
/// <param name="type"> The type of the declared service. Default is <see cref="GattServiceType.Primary"/> </param>
public sealed class ServiceDeclaration(BleUuid uuid, GattServiceType type = GattServiceType.Primary)
    : IGattServiceDeclaration
{
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;
}
