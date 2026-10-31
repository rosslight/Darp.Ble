using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary>A typed characteristic declaration.</summary>
/// <param name="uuid">The characteristic UUID.</param>
/// <param name="decode">Turns the bytes of the characteristic into the value.</param>
/// <param name="encode">Turns the value into the bytes of the characteristic.</param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public class TypedCharacteristicDeclaration<T, TProp1>(
    BleUuid uuid,
    IGattTypedCharacteristic<T>.DecodeFunc decode,
    IGattTypedCharacteristic<T>.EncodeFunc encode
) : ITypedCharacteristicDeclaration<T, TProp1>
    where TProp1 : IBleProperty
{
    private readonly IGattTypedCharacteristic<T>.DecodeFunc _decode = decode;
    private readonly IGattTypedCharacteristic<T>.EncodeFunc _encode = encode;

    /// <inheritdoc />
    public virtual GattProperty Properties => TProp1.GattProperty;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Decode" />
    protected internal T Decode(ReadOnlySpan<byte> source) => _decode(source);

    /// <inheritdoc cref="IGattTypedCharacteristic{T}.Encode" />
    protected internal byte[] Encode(T value) => _encode(value);

    T IGattTypedCharacteristic<T>.Decode(ReadOnlySpan<byte> source) => Decode(source);

    byte[] IGattTypedCharacteristic<T>.Encode(T value) => Encode(value);
}

/// <summary>A typed characteristic declaration with two properties.</summary>
/// <param name="uuid">The characteristic UUID.</param>
/// <param name="decode">Turns the bytes of the characteristic into the value.</param>
/// <param name="encode">Turns the value into the bytes of the characteristic.</param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
/// <typeparam name="TProp2"> The type of the second property </typeparam>
public sealed class TypedCharacteristicDeclaration<T, TProp1, TProp2>(
    BleUuid uuid,
    IGattTypedCharacteristic<T>.DecodeFunc decode,
    IGattTypedCharacteristic<T>.EncodeFunc encode
) : TypedCharacteristicDeclaration<T, TProp1>(uuid, decode, encode), ITypedCharacteristicDeclaration<T, TProp2>
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
            characteristicDeclaration.Decode,
            characteristicDeclaration.Encode
        );
    }
}
