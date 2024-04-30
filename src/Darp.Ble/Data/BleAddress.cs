using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The ble address </summary>
public sealed record BleAddress : ISpanParsable<BleAddress>
{
    /// <summary> Initializes a new ble address </summary>
    /// <param name="value"> The 48bit address </param>
    [SetsRequiredMembers]
    public BleAddress(UInt48 value) : this(BleAddressType.NotAvailable, value) {}

    /// <summary> Initializes a new ble address with a given type </summary>
    /// <param name="type"> The type of the address </param>
    /// <param name="value"> The 48bit address </param>
    [SetsRequiredMembers]
    public BleAddress(BleAddressType type, UInt48 value)
    {
        Type = type;
        Value = value;
    }

    /// <summary> The type of the address </summary>
    public required BleAddressType Type { get; init; }
    /// <summary> The 48bit address value </summary>
    public UInt48 Value { get; }

    /// <summary> Convert the ble address to it's underlying value </summary>
    /// <param name="bleAddress"> The ble address </param>
    /// <returns> The 48 bit address </returns>
    public static implicit operator UInt48(BleAddress bleAddress) => bleAddress.Value;
    /// <summary> Convert the 48bit value to a ble address </summary>
    /// <param name="value"> The 48 bit address </param>
    /// <returns> A ble address </returns>
    public static explicit operator BleAddress(UInt48 value) => new(value);

    /// <summary> Parses a string of suitable format and returns a ble address </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />. Will be ignored</param>
    /// <example>AA:BB:CC:DD:EE:FF</example>
    /// <returns> The parsed BleAddress with type <see cref="BleAddressType.NotAvailable"/> </returns>
    /// <exception cref="FormatException"> Thrown if the format does not comply. </exception>
    public static BleAddress Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TryParse(s, provider, out BleAddress? address)
        ? address
        : throw new FormatException($"Given input '{s}' could not be parsed");

    /// <summary> Parses a string of suitable format and returns a ble address </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />. Will be ignored</param>
    /// <param name="result"> The parsed BleAddress with type <see cref="BleAddressType.NotAvailable"/> or default </param>
    /// <example>AA:BB:CC:DD:EE:FF</example>
    /// <returns> True if the parsing was successful </returns>
    public static bool TryParse(ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleAddress? result)
    {
        if (s.Length < 17)
        {
            result = default;
            return false;
        }
        if (s[2] != ':' || s[5] != ':' || s[8] != ':' || s[11] != ':' || s[14] != ':')
        {
            result = default;
            return false;
        }
        if (!TryGetHexVal(s, out byte b0)
            || !TryGetHexVal(s[3..], out byte b1)
            || !TryGetHexVal(s[6..], out byte b2)
            || !TryGetHexVal(s[9..], out byte b3)
            || !TryGetHexVal(s[12..], out byte b4)
            || !TryGetHexVal(s[15..], out byte b5))
        {
            result = default;
            return false;
        }
        var value = new UInt48(b5, b4, b3, b2, b1, b0);
        result = new BleAddress(BleAddressType.NotAvailable, value);
        return true;
    }

    private static bool TryGetHexVal(in ReadOnlySpan<char> s, out byte res)
    {
        int lower = GetHexVal(s[1]);
        int upper = GetHexVal(s[0]);
        if (lower > 15 || upper > 15)
        {
            res = default;
            return false;
        }
        res = (byte)((upper << 4) + lower);
        return true;
    }

    private static int GetHexVal(char hex)
    {
        int val = hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : val < 97 ? 55 : 87);
    }

    /// <inheritdoc cref="Parse(ReadOnlySpan{char},System.IFormatProvider?)"/>
    public static BleAddress Parse(string s, IFormatProvider? provider) => Parse((ReadOnlySpan<char>)s, provider);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char},System.IFormatProvider?,out BleAddress?)"/>
    public static bool TryParse([NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleAddress? result)
    {
        if (s is not null) return TryParse((ReadOnlySpan<char>)s, provider, out result);
        result = default;
        return false;
    }

    /// <summary> Zero BleAddress with type <see cref="BleAddressType.NotAvailable"/> </summary>
    public static BleAddress NotAvailable { get; } = new(BleAddressType.NotAvailable, UInt48.Zero);

    /// <inheritdoc />
    public override string ToString() => $"{Value:X12}";
}