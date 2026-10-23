namespace Darp.Ble.Gatt;

/// <summary> The operations of discovered typed characteristics, one overload per position of the property marker </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The overloads differ in the characteristic or declaration they take, never in their optional parameters"
)]
public static class TypedRemoteCharacteristicExtensions
{
    /// <summary> Reads and decodes the value </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The decoded value </returns>
    public static Task<T> ReadAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Read> characteristic,
        CancellationToken cancellationToken = default
    ) => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <inheritdoc cref="ReadAsync{T}(ITypedRemoteCharacteristic{T, Properties.Read}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task<T> ReadAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Read, TOther> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <inheritdoc cref="ReadAsync{T}(ITypedRemoteCharacteristic{T, Properties.Read}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task<T> ReadAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Read> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <summary> Encodes and writes the value with a write request </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    public static Task WriteAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Write> characteristic,
        T value,
        CancellationToken cancellationToken = default
    ) => WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <inheritdoc cref="WriteAsync{T}(ITypedRemoteCharacteristic{T, Properties.Write}, T, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task WriteAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Write, TOther> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <inheritdoc cref="WriteAsync{T}(ITypedRemoteCharacteristic{T, Properties.Write}, T, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task WriteAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Write> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    private static async Task<T> ReadCoreAsync<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        byte[] value = await characteristic.Characteristic.ReadAsync(cancellationToken).ConfigureAwait(false);
        return declaration.Decode(value);
    }

    private static Task WriteCoreAsync<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        T value,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        return characteristic.Characteristic.WriteAsync(declaration.Encode(value), cancellationToken);
    }
}
