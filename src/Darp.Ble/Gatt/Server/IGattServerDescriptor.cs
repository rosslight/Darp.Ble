using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerDescriptor
{
    /// <summary> The characteristic that contains this descriptor </summary>
    IGattServerCharacteristic Characteristic { get; }

    /// <summary> The <see cref="BleUuid"/> of the descriptor </summary>
    BleUuid Uuid { get; }

    /// <summary> Read a specific value from the descriptor </summary>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A task which completes when the value was read </returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-1ee24d8c-e4ce-8881-bd1a-e7127e1a5e60">Read Descriptor Value</seealso>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-ed2e63cb-15b2-140a-71c8-572cb498135a">Read Long Descriptor Value</seealso>
    Task<byte[]> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary> Write <paramref name="bytes"/> to the characteristic depending on the length, </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A Task which completes when the response was received. True, when the write operation was successful </returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-75455264-b4ab-072b-0d02-d040ecd94412">Write Descriptor Value</seealso>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/generic-attribute-profile--gatt-.html#UUID-db7d3709-37c2-7c9a-34b7-7cbb1e9182d4">Write Long Descriptor Value</seealso>
    Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken = default);
}