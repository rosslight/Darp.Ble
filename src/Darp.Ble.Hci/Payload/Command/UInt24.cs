using System.Globalization;
using System.Runtime.CompilerServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary> A 24 bit integer </summary>
/// <param name="byte0"> The first byte </param>
/// <param name="byte1"> The second byte </param>
/// <param name="byte2"> The third byte </param>
[BinaryObject]
public readonly partial struct UInt24(byte byte0, byte byte1, byte byte2)
{
    private readonly byte _byte0 = byte0;
    private readonly byte _byte1 = byte1;
    private readonly byte _byte2 = byte2;

    /// <summary> The uint24 as a uint </summary>
    public uint ToUInt32() => _byte0 | (uint)_byte1 << 8 | (uint)_byte2 << 16;

    /// <summary> Get the uint24 from a uint </summary>
    /// <param name="value"> The uint </param>
    /// <returns> The uint24 </returns>
    public static implicit operator UInt24(uint value)
    {
        unsafe
        {
            uint* ptr = &value;
            return Unsafe.Read<UInt24>(ptr);
        }
    }

    /// <summary> Get the uint24 as uint </summary>
    /// <param name="value"> The uint24 </param>
    /// <returns> The uint </returns>
    public static implicit operator uint(UInt24 value) => value.ToUInt32();

    /// <inheritdoc />
    public override string ToString() => ToUInt32().ToString(CultureInfo.InvariantCulture);
}
