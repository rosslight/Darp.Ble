using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents errors that happened during an operation on a <see cref="IGattServerCharacteristic"/>
/// </summary>
/// <param name="characteristic"> The characteristic that caused the error </param>
/// <param name="message"> The message that describes the error. </param>
public sealed class GattCharacteristicException(IGattServerCharacteristic characteristic, string message) : Exception(message)
{
    /// <summary> The characteristic that caused the error </summary>
    public IGattServerCharacteristic Characteristic { get; } = characteristic;
}