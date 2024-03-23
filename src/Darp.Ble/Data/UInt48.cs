using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Darp.Ble.Data;

/// <summary> A 48 bit unsigned integer </summary>
public readonly struct UInt48 : IComparable<UInt48>, IMinMaxValue<UInt48>
{
    private readonly byte _b0;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private readonly byte _b1;
    private readonly byte _b2;
    private readonly byte _b3;
    private readonly byte _b4;
    private readonly byte _b5;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    /// <summary> Initialize a new 48 bit integer based on every byte field </summary>
    /// <param name="b0"> The first byte </param>
    /// <param name="b1"> The second byte </param>
    /// <param name="b2"> The third byte </param>
    /// <param name="b3"> The fourth byte </param>
    /// <param name="b4"> The fifth byte </param>
    /// <param name="b5"> The sixth byte </param>
    public UInt48(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5) =>
        (_b0, _b1, _b2, _b3, _b4, _b5) = (b0, b1, b2, b3, b4, b5);

    /// <summary> Cast a ulong to a 48 bit integer</summary>
    /// <param name="value"> The ulong to cast </param>
    /// <returns> The resulting 48 bit integer </returns>
    public static explicit operator UInt48(ulong value)
    {
        unsafe
        {
            ulong* valuePtr = &value;
            var resPtr = (UInt48*)valuePtr;
            return *resPtr;
        }
    }

    /// <summary> Cast a 48 bit integer to a ulong </summary>
    /// <param name="value"> The 48 bit integer </param>
    /// <returns> A ulong </returns>
    public static implicit operator ulong(UInt48 value)
    {
        unsafe
        {
            UInt48* valuePtr = &value;
            var resPtr = (ulong*)valuePtr;
            return *resPtr;
        }
    }

    /// <summary>Reverses a primitive value by performing an endianness swap of the specified <see cref="UInt48" /> value.</summary>
    /// <param name="value">The value to reverse.</param>
    /// <returns>The reversed value.</returns>
    public static UInt48 ReverseEndianness(UInt48 value)
    {
        return new UInt48(value._b5, value._b4, value._b3, value._b2, value._b1, value._b0);
    }

    /// <summary> Read a given source in little endian </summary>
    /// <param name="source"> The source </param>
    /// <returns> The 48 bit integer </returns>
    /// <exception cref="ArgumentOutOfRangeException"> The source does not yield enough bytes </exception>
    public static UInt48 ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        if (source.Length < 6) throw new ArgumentOutOfRangeException(nameof(source), "Source has to be 6 bytes long");
        var result = MemoryMarshal.Read<UInt48>(source);
        return BitConverter.IsLittleEndian ? result : ReverseEndianness(result);
    }

    /// <summary> Write a given 48 bit integer into a destination buffer </summary>
    /// <param name="destination"> The destination </param>
    /// <param name="uint48"> The 48 bit integer to write </param>
    /// <exception cref="ArgumentOutOfRangeException"> The destination is not long enough </exception>
    public static void WriteLittleEndian(Span<byte> destination, UInt48 uint48)
    {
        if (destination.Length < 6) throw new ArgumentOutOfRangeException(nameof(destination), "Source has to be 6 bytes long");
        UInt48 valueToWrite = BitConverter.IsLittleEndian ? uint48 : ReverseEndianness(uint48);
        MemoryMarshal.Write(destination, in valueToWrite);
    }

    /// <inheritdoc />
    public override string ToString() => ((ulong)this).ToString();

    /// <inheritdoc />
    public int CompareTo(UInt48 other)
    {
        int b0Comparison = _b0.CompareTo(other._b0);
        if (b0Comparison != 0) return b0Comparison;
        int b1Comparison = _b1.CompareTo(other._b1);
        if (b1Comparison != 0) return b1Comparison;
        int b2Comparison = _b2.CompareTo(other._b2);
        if (b2Comparison != 0) return b2Comparison;
        int b3Comparison = _b3.CompareTo(other._b3);
        if (b3Comparison != 0) return b3Comparison;
        int b4Comparison = _b4.CompareTo(other._b4);
        if (b4Comparison != 0) return b4Comparison;
        return _b5.CompareTo(other._b5);
    }

    /// <inheritdoc />
    public static UInt48 MaxValue { get; } = new(0xFF,0xFF,0xFF,0xFF,0xFF,0xFF);

    /// <inheritdoc />
    public static UInt48 MinValue { get; } = new(0x00,0x00,0x00,0x00,0x00,0x00);
}