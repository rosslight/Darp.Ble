using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary> A 48 bit device address </summary>
/// <param name="byte0"></param>
/// <param name="byte1"></param>
/// <param name="byte2"></param>
/// <param name="byte3"></param>
/// <param name="byte4"></param>
/// <param name="byte5"></param>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DeviceAddress(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5)
    : IDecodable<DeviceAddress>
{
    private readonly byte _byte0 = byte0;
    private readonly byte _byte1 = byte1;
    private readonly byte _byte2 = byte2;
    private readonly byte _byte3 = byte3;
    private readonly byte _byte4 = byte4;
    private readonly byte _byte5 = byte5;

    /// <summary> The address as a ulong </summary>
    public ulong Address => _byte0 | (ulong)_byte1 << 8 | (ulong)_byte2 << 16
                            | (ulong)_byte3 << 24 | (ulong)_byte4 << 32 | (ulong)_byte5 << 40;

    /// <summary> Get te device address from a ulong </summary>
    /// <param name="value"> The ulong </param>
    /// <returns> The device address </returns>
    public static implicit operator DeviceAddress(ulong value)
    {
        unsafe
        {
            ulong* ptr = &value;
            return Unsafe.Read<DeviceAddress>(ptr);
        }
    }

    /// <summary> Get the address as ulong </summary>
    /// <param name="address"> The address </param>
    /// <returns> The ulong </returns>
    public static implicit operator ulong(DeviceAddress address) => address.Address;

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out DeviceAddress result, out int bytesDecoded)
    {
        bytesDecoded = 6;
        result = default;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        result = new DeviceAddress(span[0], span[1], span[2], span[3], span[4], span[5]);
        return true;
    }
}