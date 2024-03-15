namespace Darp.Ble.Data;

public struct UInt48 : IComparable<UInt48>
{
    private readonly byte _b0;
    private readonly byte _b1;
    private readonly byte _b2;
    private readonly byte _b3;
    private readonly byte _b4;
    private readonly byte _b5;

    public static explicit operator UInt48(ulong value)
    {
        unsafe
        {
            ulong* valuePtr = &value;
            var resPtr = (UInt48*)valuePtr;
            return *resPtr;
        }
    }
    public static implicit operator ulong(UInt48 value)
    {
        unsafe
        {
            UInt48* valuePtr = &value;
            var resPtr = (ulong*)valuePtr;
            return *resPtr;
        }
    }

    public static UInt48 ReadLitteEndian(ReadOnlySpan<byte> source)
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

    public static void WriteLitteEndian(Span<byte> destination, UInt48 uint48)
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