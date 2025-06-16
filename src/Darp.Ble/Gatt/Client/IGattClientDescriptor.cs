using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;

namespace Darp.Ble.Gatt.Client;

/// <summary> The descriptor </summary>
public interface IGattClientDescriptor
{
    /// <summary> The characteristic this descriptor was added to </summary>
    IGattClientCharacteristic Characteristic { get; }

    /// <summary> The UUID of the descriptor </summary>
    BleUuid Uuid { get; }

    /// <summary> Access the descriptor declaration </summary>
    internal IGattCharacteristicValue Value { get; }
}
