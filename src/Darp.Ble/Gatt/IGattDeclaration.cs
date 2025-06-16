using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A gatt attribute declaration </summary>
public interface IGattDeclaration
{
    /// <summary> The uuid of the given declaration </summary>
    BleUuid Uuid { get; }
}
