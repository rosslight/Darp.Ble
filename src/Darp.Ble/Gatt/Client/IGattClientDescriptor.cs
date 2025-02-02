using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientDescriptor : IGattClientAttribute
{
    /// <summary> The characteristic this descriptor was added to </summary>
    IGattClientCharacteristic Characteristic { get; }

    /// <summary> The UUID of the descriptor </summary>
    BleUuid Uuid { get; }
}