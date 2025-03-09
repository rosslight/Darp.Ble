using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientDescriptor
{
    /// <summary> The characteristic this descriptor was added to </summary>
    IGattClientCharacteristic Characteristic { get; }

    /// <summary> The UUID of the descriptor </summary>
    BleUuid Uuid { get; }

    /// <summary> Access the descriptor declaration </summary>
    internal IGattCharacteristicValue Value { get; }
}
