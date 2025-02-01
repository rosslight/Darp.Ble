using System.Runtime.InteropServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Services;

namespace Darp.Ble.Gatt;

public static class CharacteristicDeclaration
{
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

    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(BleUuid uuid,
        IGattAttributeDeclaration<T>.ReadValueFunc onRead,
        IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(uuid, onRead, onWrite);
    }
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid,
        IGattAttributeDeclaration<T>.ReadValueFunc onRead,
        IGattAttributeDeclaration<T>.WriteValueFunc onWrite)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(uuid, onRead, onWrite);
    }

    public static TypedCharacteristicDeclaration<T, TProp1> Create<T, TProp1>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }
    public static TypedCharacteristicDeclaration<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristicDeclaration<T, TProp1, TProp2>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }

    public static TypedCharacteristicDeclaration<string, TProp1> Create<TProp1>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1>(uuid, encoding.GetString, encoding.GetBytes);
    }
    public static TypedCharacteristicDeclaration<string, TProp1, TProp2> Create<TProp1, TProp2>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1, TProp2>(uuid, encoding.GetString, encoding.GetBytes);
    }
}