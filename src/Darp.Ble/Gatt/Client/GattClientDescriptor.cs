using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client descriptor </summary>
/// <param name="clientCharacteristic"> The parent client characteristic </param>
/// <param name="value"> The descriptor value </param>
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
