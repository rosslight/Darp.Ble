using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A typed characteristic declaration </summary>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public interface ITypedCharacteristicDeclaration<T, TProp1> : ICharacteristicDeclaration, IGattTypedCharacteristic<T>
    where TProp1 : IBleProperty;

/// <summary> The typed characteristic declaration </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public class TypedCharacteristicDeclaration<T, TProp1>(
    BleUuid uuid,
    IGattTypedCharacteristic<T>.DecodeFunc onRead,
    IGattTypedCharacteristic<T>.EncodeFunc onWrite
) : ITypedCharacteristicDeclaration<T, TProp1>
    where TProp1 : IBleProperty
{
    private readonly IGattTypedCharacteristic<T>.DecodeFunc _onRead = onRead;
    private readonly IGattTypedCharacteristic<T>.EncodeFunc _onWrite = onWrite;

    /// <inheritdoc />
    public virtual GattProperty Properties => TProp1.GattProperty;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Decode" />
    protected internal T ReadValue(ReadOnlySpan<byte> source) => _onRead(source);

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Encode" />
    protected internal byte[] WriteValue(T value) => _onWrite(value);

    T IGattTypedCharacteristic<T>.Decode(ReadOnlySpan<byte> source) => ReadValue(source);

    byte[] IGattTypedCharacteristic<T>.Encode(T value) => WriteValue(value);
}

/// <summary> The typed characteristic declaration </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
/// <typeparam name="TProp2"> The type of the second property </typeparam>
public sealed class TypedCharacteristicDeclaration<T, TProp1, TProp2>(
    BleUuid uuid,
    IGattTypedCharacteristic<T>.DecodeFunc onRead,
    IGattTypedCharacteristic<T>.EncodeFunc onWrite
) : TypedCharacteristicDeclaration<T, TProp1>(uuid, onRead, onWrite), ITypedCharacteristicDeclaration<T, TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <inheritdoc />
    public override GattProperty Properties => TProp1.GattProperty | TProp2.GattProperty;

    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator TypedCharacteristicDeclaration<T, TProp2, TProp1>(
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration
    )
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new TypedCharacteristicDeclaration<T, TProp2, TProp1>(
            characteristicDeclaration.Uuid,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue
        );
    }
}
