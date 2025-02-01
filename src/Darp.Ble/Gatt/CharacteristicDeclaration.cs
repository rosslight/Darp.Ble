using System.Runtime.InteropServices;
using System.Text;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The characteristic declaration </summary>
public interface IGattCharacteristicDeclaration
{
    /// <summary> Properties that are part of the characteristic declaration </summary>
    GattProperty Properties { get; }
}

/// <summary> The characteristic declaration with specified properties </summary>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public interface IGattCharacteristicDeclaration<TProp1>
    : IGattCharacteristicDeclaration, IGattAttributeDeclaration
    where TProp1 : IBleProperty;

/// <summary> The characteristic declaration </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="TProp1"> The type of the first property </typeparam>
public class CharacteristicDeclaration<TProp1>(BleUuid uuid) : IGattCharacteristicDeclaration<TProp1>
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
    : CharacteristicDeclaration<TProp1>(uuid), IGattCharacteristicDeclaration<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <inheritdoc />
    public override GattProperty Properties => TProp1.GattProperty | TProp2.GattProperty;

    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    public static implicit operator CharacteristicDeclaration<TProp2, TProp1>(
        CharacteristicDeclaration<TProp1, TProp2> characteristicDeclaration)
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
    /// <param name="onRead"> The callback to read a value from bytes </param>
    /// <param name="onWrite"> The callback to write a value to bytes </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(BleUuid uuid,
        IGattAttributeDeclaration<T>.ReadValueFunc onRead,
        IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(uuid, onRead, onWrite);
    }

    /// <summary> Create a new typed characteristic declaration </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <param name="onRead"> The callback to read a value from bytes </param>
    /// <param name="onWrite"> The callback to write a value to bytes </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid,
        IGattAttributeDeclaration<T>.ReadValueFunc onRead,
        IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(uuid, onRead, onWrite);
    }

    /// <summary> Create a new typed characteristic declaration for an unmanaged value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }

    /// <summary> Create a new typed characteristic declaration for an unmanaged value </summary>
    /// <param name="uuid"> The uuid of the characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A typed characteristic declaration </returns>
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
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
    public static TypedCharacteristicDeclaration<string, TProp1, TProp2> Create<TProp1, TProp2>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1, TProp2>(uuid, encoding.GetString, encoding.GetBytes);
    }

    private static T ToStruct<T>(this in ReadOnlySpan<byte> bytes) where T : unmanaged => MemoryMarshal.Read<T>(bytes);
    private static byte[] ToByteArray<T>(this T value)
        where T : unmanaged
    {
        int size = typeof(T).IsEnum
            ? Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T)))
            : Marshal.SizeOf<T>();
        var buffer = new byte[size];
        MemoryMarshal.Write(buffer, value);
        return buffer;
    }
}