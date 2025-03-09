using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client descriptor </summary>
/// <param name="clientCharacteristic"> The parent client characteristic </param>
/// <param name="uuid"> The UUID of the descriptor </param>
/// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
/// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
public abstract class GattClientDescriptor(
    GattClientCharacteristic clientCharacteristic,
    IGattCharacteristicValue value
) : IGattClientDescriptor
{
    /// <inheritdoc />
    public BleUuid Uuid => Value.AttributeType;

    /// <inheritdoc />
    public IGattCharacteristicValue Value { get; } = value;

    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = clientCharacteristic;
}
