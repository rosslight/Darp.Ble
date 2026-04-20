using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary>
/// Represents a descriptor exposed by a GATT server characteristic.
/// </summary>
public interface IGattServerDescriptor
{
    /// <summary>Gets the characteristic that contains this descriptor.</summary>
    IGattServerCharacteristic Characteristic { get; }

    /// <summary>Gets the descriptor UUID.</summary>
    BleUuid Uuid { get; }

    /// <summary>Reads the current descriptor value.</summary>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>A task that returns the descriptor value.</returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-1ee24d8c-e4ce-8881-bd1a-e7127e1a5e60">Read Descriptor Value</seealso>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-ed2e63cb-15b2-140a-71c8-572cb498135a">Read Long Descriptor Value</seealso>
    Task<byte[]> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes a new value to the descriptor.</summary>
    /// <param name="bytes">The bytes to write.</param>
    /// <param name="cancellationToken">Cancels the write operation.</param>
    /// <returns>A task that returns whether the write succeeded.</returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-75455264-b4ab-072b-0d02-d040ecd94412">Write Descriptor Value</seealso>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-db7d3709-37c2-7c9a-34b7-7cbb1e9182d4">Write Long Descriptor Value</seealso>
    Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary> Write <paramref name="bytes"/> to the characteristic without expecting a response </summary>
    /// <param name="bytes"> The bytes to be sent </param>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-5bcdaffa-d6e9-9d99-01b6-dd6bacc09656">Write Without Response</seealso>
    void WriteWithoutResponse(byte[] bytes);
}
