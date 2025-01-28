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
    /// <summary> Initialize a new characteristic from a given ushort </summary>
    /// <param name="uuid"> The UUID as ushort </param>
    public Characteristic(ushort uuid) : this(new BleUuid(uuid)) {}

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; } = uuid;
    /// <summary> The property </summary>
    public GattProperty Property => TProp1.GattProperty;
}

public sealed class Characteristic<TProp1, TProp2>(BleUuid uuid) : Characteristic<TProp1>(uuid), ICharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> The property </summary>
    public GattProperty Property => TProp1.GattProperty | TProp2.GattProperty;
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
public sealed class TypedCharacteristic<T, TProp1>(BleUuid uuid,
    Func<byte[], T> onRead,
    Func<T, byte[]> onWrite) : Characteristic<TProp1>(uuid)
    where TProp1 : IBleProperty
{
    /// <summary> Initialize a new characteristic from a given ushort </summary>
    /// <param name="uuid"> The UUID as ushort </param>
    public TypedCharacteristic(ushort uuid,
        Func<byte[], T> onRead,
        Func<T, byte[]> onWrite) : this(new BleUuid(uuid), onRead, onWrite) {}

    public Func<byte[], T> OnRead { get; } = onRead;
    public Func<T, byte[]> OnWrite { get; } = onWrite;
}

public static class Characteristic
{
    public static T ToStruct<T>(this byte[] bytes) where T : unmanaged => MemoryMarshal.Read<T>(bytes);

    public static byte[] ToByteArray<T>(this T value)
        where T : unmanaged
    {
        var buffer = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(buffer, value);
        return buffer;
    }

    public static TypedCharacteristic<T, TProp1> Create<T, TProp1>(ushort uuid,
        Func<byte[], T> onRead,
        Func<T, byte[]> onWrite)
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1>(uuid, onRead, onWrite);
    }

    public static TypedCharacteristic<T, TProp1> Create<T, TProp1>(ushort uuid)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        return new TypedCharacteristic<T, TProp1>(uuid, bytes => bytes.ToStruct<T>(), value => value.ToByteArray());
    }

    public static TypedCharacteristic<string, TProp1> Create<TProp1>(ushort uuid, Encoding encoding)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return Create<string, TProp1>(uuid, encoding.GetString, encoding.GetBytes);
    }
}