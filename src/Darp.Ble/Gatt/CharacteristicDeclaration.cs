using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The characteristic declaration </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public class CharacteristicDeclaration<TProp1>(BleUuid uuid) : ICharacteristicDeclaration<TProp1>
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public virtual GattProperty Properties => TProp1.GattProperty;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
}

/// <summary> The characteristic declaration </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
/// <typeparam name="TProp2"> The type of the second property </typeparam>
public sealed class CharacteristicDeclaration<TProp1, TProp2>(BleUuid uuid)
    : CharacteristicDeclaration<TProp1>(uuid),
        ICharacteristicDeclaration<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <inheritdoc />
    public override GattProperty Properties => TProp1.GattProperty | TProp2.GattProperty;

    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator CharacteristicDeclaration<TProp2, TProp1>(
        CharacteristicDeclaration<TProp1, TProp2> characteristicDeclaration
    )
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new CharacteristicDeclaration<TProp2, TProp1>(characteristicDeclaration.Uuid);
    }
}

/// <summary> Helper methods for creating a new characteristic declaration </summary>
public static class CharacteristicDeclaration
{
    /// <summary> Create a new typed characteristic declaration </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <param name="decode"> Turns the bytes of the characteristic into the value </param>
    /// <param name="encode"> Turns the value into the bytes of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(
        BleUuid uuid,
        IGattTypedCharacteristic<T>.DecodeFunc decode,
        IGattTypedCharacteristic<T>.EncodeFunc encode
    )
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(uuid, decode, encode);
    }

    /// <summary> Create a new typed characteristic declaration </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <param name="decode"> Turns the bytes of the characteristic into the value </param>
    /// <param name="encode"> Turns the value into the bytes of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(
        BleUuid uuid,
        IGattTypedCharacteristic<T>.DecodeFunc decode,
        IGattTypedCharacteristic<T>.EncodeFunc encode
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(uuid, decode, encode);
    }

    /// <summary> Create a new typed characteristic declaration for an unmanaged value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <remarks>
    /// The value goes on the wire as a copy of its memory: <c>Unsafe.SizeOf&lt;T&gt;()</c> bytes in the machine's byte order,
    /// which is little endian like GATT on the platforms this library runs on, and a struct's padding with it.
    /// That fits primitives, enums and packed structs; for other structured values, pass a decode and an encode.
    /// Decoding a value of another size throws a <see cref="GattException"/> (InvalidAttributeValueLength)
    /// </remarks>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(
            uuid,
            bytes => bytes.ToStruct<T>(),
            value => value.ToByteArray()
        );
    }

    /// <summary> Create a new typed characteristic declaration for an unmanaged value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <remarks>
    /// The value goes on the wire as a copy of its memory: <c>Unsafe.SizeOf&lt;T&gt;()</c> bytes in the machine's byte order,
    /// which is little endian like GATT on the platforms this library runs on, and a struct's padding with it.
    /// That fits primitives, enums and packed structs; for other structured values, pass a decode and an encode.
    /// Decoding a value of another size throws a <see cref="GattException"/> (InvalidAttributeValueLength)
    /// </remarks>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(
            uuid,
            bytes => bytes.ToStruct<T>(),
            value => value.ToByteArray()
        );
    }

    /// <summary> Create a new typed characteristic declaration for a string value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <param name="encoding"> The encoding to be used when transforming the string </param>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<string, TProp1> Create<TProp1>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1>(uuid, encoding.GetString, encoding.GetBytes);
    }

    /// <summary> Create a new typed characteristic declaration for a string value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <param name="encoding"> The encoding to be used when transforming the string </param>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<string, TProp1, TProp2> Create<TProp1, TProp2>(
        BleUuid uuid,
        Encoding encoding
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1, TProp2>(uuid, encoding.GetString, encoding.GetBytes);
    }

    /// <summary> Reads a value of exactly the size of <typeparamref name="T"/> </summary>
    /// <exception cref="GattException"> The value has another size (InvalidAttributeValueLength) </exception>
    private static T ToStruct<T>(this in ReadOnlySpan<byte> bytes)
        where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        if (bytes.Length != size)
        {
            throw new GattException(
                GattProtocolStatus.InvalidAttributeValueLength,
                $"The value has {bytes.Length} bytes instead of {size}"
            );
        }
        return MemoryMarshal.Read<T>(bytes);
    }

    private static byte[] ToByteArray<T>(this T value)
        where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        var buffer = new byte[size];
        MemoryMarshal.Write(buffer, value);
        return buffer;
    }
}
