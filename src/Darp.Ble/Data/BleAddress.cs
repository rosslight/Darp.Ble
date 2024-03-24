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
        : throw new FormatException("Expected input to be follow 'AA:BB:CC:DD:EE:FF' (length of 17) but is invalid");

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
        var value = new UInt48(
            GetHexVal(s),
            GetHexVal(s[3..]),
            GetHexVal(s[6..]),
            GetHexVal(s[9..]),
            GetHexVal(s[12..]),
            GetHexVal(s[15..])
        );
        result = new BleAddress(BleAddressType.NotAvailable, value);
        return true;
    }

    private static byte GetHexVal(in ReadOnlySpan<char> s) => (byte)((GetHexVal(s[0]) << 4) + GetHexVal(s[1]));

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
}