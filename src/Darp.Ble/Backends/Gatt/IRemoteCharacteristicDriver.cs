using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A characteristic discovered on a peripheral. Belongs to its connection, which returns the same object for the same characteristic every time </summary>
public interface IRemoteCharacteristicDriver
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    GattProperty Properties { get; }

    /// <summary> Reads the value; a long value takes several transactions </summary>
    /// <param name="request"> Reports each transaction; check <see cref="GattRequest.IsAbandoned"/> before the next one </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task<byte[]> ReadAsync(GattRequest request);

    /// <summary> Writes the value with a write request; a long value takes several transactions </summary>
    /// <param name="value"> The value; the core keeps it unchanged until the task completes </param>
    /// <param name="request"> Reports each transaction; once abandoned, the driver finishes the current one and cancels a prepared write </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request);
}
