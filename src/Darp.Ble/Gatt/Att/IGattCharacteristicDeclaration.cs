using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Att;

/// <summary> A gatt characteristic attribute declaration </summary>
public interface IGattCharacteristicDeclaration : IGattAttribute
{
    /// <summary> The property of the characteristic </summary>
    GattProperty Properties { get; }
}
