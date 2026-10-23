using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A service declaration </summary>
/// <param name="uuid"> The uuid of the declared service </param>
/// <param name="type"> The type of the declared service. Default is <see cref="GattServiceType.Primary"/> </param>
public sealed class ServiceDeclaration(BleUuid uuid, GattServiceType type = GattServiceType.Primary)
    : IServiceDeclaration
{
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;
}
