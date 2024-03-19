namespace Darp.Ble.Utils;

/// <summary> Guid extensions </summary>
public static class GuidExtensions
{
    public static Guid ToBleGuid(this ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 16)
            return new Guid(bytes);

        Span<byte> baseUuidBytes = stackalloc byte[] { 0,0,0,0,0,0,0,16,128,0,0,128,95,155,52,251 };
        switch (bytes.Length)
        {
            case 2:
                baseUuidBytes[0] = bytes[0];
                baseUuidBytes[1] = bytes[1];
                break;
            case 4:
                baseUuidBytes[0] = bytes[0];
                baseUuidBytes[1] = bytes[1];
                baseUuidBytes[2] = bytes[2];
                baseUuidBytes[3] = bytes[3];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bytes), $"Provided invalid number of bytes for guid: {bytes.Length}");
        }
        return new Guid(baseUuidBytes);
    }

    public static Guid ToBleGuid(this byte[] bytes) => new ReadOnlySpan<byte>(bytes).ToBleGuid();

    public static bool IsValidBleGuidLength(this byte[] bytes) => new[] { 2, 4, 16 }.Contains(bytes.Length);

    /// <summary>
    /// A string containing a hex number array of sizes 2, 4, 8, 16.
    /// '-' character is allowed for separation but will have no further effect. <br/>
    ///  2 bytes ("1122") -> "00001122-0000-1000-8000-00805f9b34fb" <br/>
    /// </summary>
    /// <param name="bluetoothUuid">The string to be parsed</param>
    /// <returns>A guid matching the ble specification</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if number of bytes is not even</exception>
    public static Guid ToBleGuid(this string bluetoothUuid)
    {
        bluetoothUuid = bluetoothUuid.Replace("-", "");
        Span<byte> bytes = stackalloc byte[bluetoothUuid.Length >> 1];
        bluetoothUuid.WriteByteArray(bytes);
        if (bytes.Length % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(bluetoothUuid), $"even number of bytes necessary, got {bytes.Length}");
        if (bytes.Length >= 8) (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
        if (bytes.Length >= 6) (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
        if (bytes.Length >= 4) (bytes[0], bytes[1], bytes[2], bytes[3]) = (bytes[3], bytes[2], bytes[1], bytes[0]);
        else if (bytes.Length >= 2) (bytes[0], bytes[1]) = (bytes[1], bytes[0]);
        // reverse bytes
        //for (var i = 0; i < bytes.Length / 2; i++)
        //    (bytes[^(i + 1)], bytes[i]) = (bytes[i], bytes[^(i + 1)]);
        return ToBleGuid(bytes);
    }

    // TODO public static Guid ToBleGuid(this GattUuid uuid) => ToBleGuid((ushort)uuid);

    public static Guid ToBleGuid(this ushort uuid)
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[]
        {
            (byte)uuid,(byte)(uuid >> 8),0,0,0,0,0,16,128,0,0,128,95,155,52,251
        };
        return bytes.ToBleGuid();
    }
    public static bool Contains(this IEnumerable<Guid> guids, ushort uuid)
    {
        foreach (Guid guid in guids)
        {
            unsafe
            {
                var pGuid = (ushort*)&guid;
                if (*pGuid == uuid)
                    return true;
            }
        }
        return false;
    }

    // TODO public static GattUuid ToGattUuid(this Guid guid) => (GattUuid)guid.ToUInt16();

    public static ushort ToUInt16(this Guid guid)
    {
        unsafe
        {
            ushort value = *(ushort*)&guid;
            return value;
        }
    }

    // TODO public static bool ContainsKey<T>(this IDictionary<Guid, T> dict, GattUuid uuid) => dict.ContainsKey((ushort)uuid);

    public static bool ContainsKey<T>(this IDictionary<Guid, T> dict, ushort uuid)
    {
        unsafe
        {
            ReadOnlySpan<ushort> s = stackalloc ushort[] { uuid, 0x0, 0x0, 0x1000, 0x2902, 0x8000, 0x9b5f, 0xfb34};
            fixed (ushort* pSpan = &s.GetPinnableReference())
            {
                Guid g = *(Guid*)pSpan;
                return dict.ContainsKey(g);
            }
        }
    }

    // TODO public static T Get<T>(this IDictionary<Guid, T> dict, GattUuid uuid)
    // {
    //     foreach ((Guid key, T? value) in dict)
    //     {
    //         if (key.ToGattUuid() == uuid)
    //             return value;
    //     }
    //     throw new KeyNotFoundException($"Default guid {uuid} is not contained in characteristic");
    // }
// 
    // public static bool Contains(this IEnumerable<Guid> guids, GattUuid uuid) => guids.Contains((ushort)uuid);
}