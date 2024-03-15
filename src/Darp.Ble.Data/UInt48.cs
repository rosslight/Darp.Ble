namespace Darp.Ble.Data;

/// <summary> A 48 bit unsigned integer </summary>
public struct UInt48 : IComparable<UInt48>
{
    private readonly byte _b0;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private readonly byte _b1;
    private readonly byte _b2;
    private readonly byte _b3;
    private readonly byte _b4;
    private readonly byte _b5;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    /// <summary> Cast a ulong to </summary>
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

    /// <summary> Read a given source in little endian </summary>
    /// <param name="source"> The source </param>
    /// <returns> The 48 bit integer </returns>
    /// <exception cref="NotImplementedException"> Non little endian systems are not implemented yet </exception>
    /// <exception cref="ArgumentOutOfRangeException"> The source does not yield enough bytes </exception>
    public static UInt48 ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        if (!BitConverter.IsLittleEndian) throw new NotImplementedException();
        if (source.Length < 6) throw new ArgumentOutOfRangeException(nameof(source), "Source has to be 6 bytes long");
        unsafe
        {
            fixed (byte* sourcePtr = source)
            {
                var resPtr = (UInt48*)sourcePtr;
                return *resPtr;
            }
        }
    }

    /// <summary> Write a given 48 bit integer into a destination buffer </summary>
    /// <param name="destination"> The destination </param>
    /// <param name="uint48"> The 48 bit integer to write </param>
    /// <exception cref="NotImplementedException"> Non little endian systems are not implemented yet </exception>
    /// <exception cref="ArgumentOutOfRangeException"> The destination is not long enough </exception>
    public static void WriteLittleEndian(Span<byte> destination, UInt48 uint48)
    {
        if (!BitConverter.IsLittleEndian) throw new NotImplementedException();
        if (destination.Length < 6) throw new ArgumentOutOfRangeException(nameof(destination), "Source has to be 6 bytes long");
        unsafe
        {
            byte* sourcePtr = &uint48._b0;
            new ReadOnlySpan<byte>(sourcePtr, 6).CopyTo(destination);
        }
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
}