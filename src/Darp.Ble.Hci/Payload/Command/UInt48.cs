using System.Runtime.CompilerServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary> A 48 bit integer </summary>
/// <param name="byte0"> The first byte </param>
/// <param name="byte1"> The second byte </param>
/// <param name="byte2"> The third byte </param>
/// <param name="byte3"> The fourth byte </param>
/// <param name="byte4"> The firth byte </param>
/// <param name="byte5"> The sixth byte </param>
[BinaryObject]
public readonly partial struct UInt48(
    byte byte0,
    byte byte1,
    byte byte2,
    byte byte3,
    byte byte4,
    byte byte5
)
{
    private readonly byte _byte0 = byte0;
    private readonly byte _byte1 = byte1;
    private readonly byte _byte2 = byte2;
    private readonly byte _byte3 = byte3;
    private readonly byte _byte4 = byte4;
    private readonly byte _byte5 = byte5;

    /// <summary> The uint48 as a ulong </summary>
    public ulong ToUInt64() =>
        _byte0
        | (ulong)_byte1 << 8
        | (ulong)_byte2 << 16
        | (ulong)_byte3 << 24
        | (ulong)_byte4 << 32
        | (ulong)_byte5 << 40;

    /// <summary> Get the uint48 from a ulong </summary>
    /// <param name="value"> The ulong </param>
    /// <returns> The uint48 </returns>
    public static implicit operator UInt48(ulong value)
    {
        unsafe
        {
            ulong* ptr = &value;
            return Unsafe.Read<UInt48>(ptr);
        }
    }

    /// <summary> Get the uint48 as ulong </summary>
    /// <param name="value"> The uint48 </param>
    /// <returns> The ulong </returns>
    public static implicit operator ulong(UInt48 value) => value.ToUInt64();
}
