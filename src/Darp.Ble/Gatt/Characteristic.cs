using System.Runtime.InteropServices;
using System.Text;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic with a single property </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
public class Characteristic<TProp1>(BleUuid uuid) : ICharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; } = uuid;
    /// <summary> The property </summary>
    public virtual GattProperty Property => TProp1.GattProperty;
}

public sealed class Characteristic<TProp1, TProp2>(BleUuid uuid) : Characteristic<TProp1>(uuid), ICharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> The property </summary>
    public override GattProperty Property => TProp1.GattProperty | TProp2.GattProperty;

    public static implicit operator Characteristic<TProp2, TProp1>(
        Characteristic<TProp1, TProp2> characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return new Characteristic<TProp2, TProp1>(characteristic.Uuid);
    }
}

public interface ICharacteristic<TProp1> where TProp1 : IBleProperty
{
    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; }
}

/// <summary> A characteristic with a single property and a known type </summary>
/// <param name="uuid"> The uuid of the characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The property </typeparam>
public class TypedCharacteristic<T, TProp1>(BleUuid uuid,
    Func<byte[], T> onRead,
    Func<T, byte[]> onWrite) : Characteristic<TProp1>(uuid)
    where TProp1 : IBleProperty
{
    public Func<byte[], T> OnRead { get; } = onRead;
    public Func<T, byte[]> OnWrite { get; } = onWrite;
}

public sealed class TypedCharacteristic<T, TProp1, TProp2>(BleUuid uuid,
    Func<byte[], T> onRead,
    Func<T, byte[]> onWrite) : TypedCharacteristic<T, TProp1>(uuid, onRead, onWrite), ICharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> The property </summary>
    public override GattProperty Property => TProp1.GattProperty | TProp2.GattProperty;

    public static implicit operator TypedCharacteristic<T, TProp2, TProp1>(
        TypedCharacteristic<T, TProp1, TProp2> characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return new TypedCharacteristic<T, TProp2, TProp1>(characteristic.Uuid, characteristic.OnRead, characteristic.OnWrite);
    }
}

public static class Characteristic
{
    public static T ToStruct<T>(this byte[] bytes) where T : unmanaged => MemoryMarshal.Read<T>(bytes);

    public static byte[] ToByteArray<T>(this T value)
        where T : unmanaged
    {
        int size = typeof(T).IsEnum
            ? Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T)))
            : Marshal.SizeOf<T>();
        var buffer = new byte[size];
        MemoryMarshal.Write(buffer, value);
        return buffer;
    }

    public static TypedCharacteristic<T, TProp1> Create<T, TProp1>(BleUuid uuid,
        Func<byte[], T> onRead,
        Func<T, byte[]> onWrite)
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1>(uuid, onRead, onWrite);
    }
    public static TypedCharacteristic<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid,
        Func<byte[], T> onRead,
        Func<T, byte[]> onWrite)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1, TProp2>(uuid, onRead, onWrite);
    }

    public static TypedCharacteristic<T, TProp1> Create<T, TProp1>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }
    public static TypedCharacteristic<T, TProp1, TProp2> Create<T, TProp1, TProp2>(BleUuid uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1, TProp2>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }

    public static TypedCharacteristic<string, TProp1> Create<TProp1>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1>(uuid, encoding.GetString, encoding.GetBytes);
    }
    public static TypedCharacteristic<string, TProp1, TProp2> Create<TProp1, TProp2>(BleUuid uuid, Encoding encoding)
        where TProp1 : IBleProperty where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1, TProp2>(uuid, encoding.GetString, encoding.GetBytes);
    }
}