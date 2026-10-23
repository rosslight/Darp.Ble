namespace Darp.Ble.Gatt;

/// <summary> The operations of discovered untyped characteristics, one per property marker </summary>
public static class RemoteCharacteristicExtensions
{
    /// <summary> Reads the value </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <returns> The value </returns>
    public static Task<byte[]> ReadAsync(
        this IRemoteCharacteristic<Properties.Read> characteristic,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.ReadAsync(cancellationToken);
    }

    /// <summary> Writes the value with a write request </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value; copied, so the caller may reuse the buffer at once </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    public static Task WriteAsync(
        this IRemoteCharacteristic<Properties.Write> characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.WriteAsync(value, cancellationToken);
    }
}
