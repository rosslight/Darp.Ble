using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> An interface defining a gatt attribute with a start handle and an end handle </summary>
public interface IGattAttribute
{
    /// <summary> The start handle of the attribute </summary>
    ushort Handle { get; }

    /// <summary> The type of the attribute </summary>
    BleUuid AttributeType { get; }

    /// <summary> The value of the attribute </summary>
    byte[] AttributeValue { get; }
}
