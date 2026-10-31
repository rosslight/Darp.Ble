using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace Darp.Ble.Data;

/// <summary> The ble address </summary>
public sealed record BleAddress : ISpanParsable<BleAddress>, IFormattable, IEquatable<UInt48>, IEquatable<ulong>
{
    /// <summary> Initializes a new ble address </summary>
    /// <param name="value"> The 48bit address </param>
    [SetsRequiredMembers]
    public BleAddress(UInt48 value)
        : this(BleAddressType.NotAvailable, value) { }

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

    /// <summary> The kind of the address if the <see cref="Type"/> is <see cref="BleAddressType.Random"/> </summary>
    /// <remarks> <c>null</c> for any other type or if the two most significant bits have the reserved value 0b10 </remarks>
    public BleRandomAddressKind? RandomKind
    {
        get
        {
            if (Type is not BleAddressType.Random)
                return null;
            return (((ulong)Value >> 46) & 0b11) switch
            {
                0b00 => BleRandomAddressKind.NonResolvablePrivate,
                0b01 => BleRandomAddressKind.ResolvablePrivate,
                0b11 => BleRandomAddressKind.Static,
                _ => null,
            };
        }
    }

    /// <summary> Convert the ble address to it's underlying value </summary>
    /// <param name="bleAddress"> The ble address </param>
    /// <returns> The 48 bit address </returns>
    [SuppressMessage(
        "Usage",
        "CA2225:Operator overloads have named alternates",
        Justification = "Value is the named alternate"
    )]
    public static implicit operator UInt48(BleAddress bleAddress)
    {
        ArgumentNullException.ThrowIfNull(bleAddress);
        return bleAddress.Value;
    }

    /// <summary> Convert the 48bit value to a ble address </summary>
    /// <param name="value"> The 48 bit address </param>
    /// <returns> A ble address </returns>
    [SuppressMessage(
        "Usage",
        "CA2225:Operator overloads have named alternates",
        Justification = "The constructor is the named alternate"
    )]
    public static explicit operator BleAddress(UInt48 value) => new(value);

    /// <summary> Parses an address as <see cref="ToString(string?, IFormatProvider?)"/> writes it </summary>
    /// <param name="s"> The address: AA:BB:CC:DD:EE:FF, or with its type as P:AA:BB:CC:DD:EE:FF (public) or R:AA:BB:CC:DD:EE:FF (random) </param>
    /// <param name="provider"> Ignored </param>
    /// <returns> The address; without a type in the text, of type <see cref="BleAddressType.NotAvailable"/> </returns>
    /// <exception cref="FormatException"> Thrown if the format does not comply. </exception>
    public static BleAddress Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out BleAddress? address)
            ? address
            : throw new FormatException($"Given input '{s}' could not be parsed");

    /// <summary> Parses an address as <see cref="ToString(string?, IFormatProvider?)"/> writes it </summary>
    /// <param name="s"> The address: AA:BB:CC:DD:EE:FF, or with its type as P:AA:BB:CC:DD:EE:FF (public) or R:AA:BB:CC:DD:EE:FF (random) </param>
    /// <param name="provider"> Ignored </param>
    /// <param name="result"> The address; without a type in the text, of type <see cref="BleAddressType.NotAvailable"/> </param>
    /// <returns> True if the parsing was successful </returns>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleAddress? result
    )
    {
        result = null;
        BleAddressType type = BleAddressType.NotAvailable;
        if (s.Length == 19 && s[1] == ':')
        {
            type = s[0] switch
            {
                'P' => BleAddressType.Public,
                'R' => BleAddressType.Random,
                _ => BleAddressType.NotAvailable,
            };
            if (type is BleAddressType.NotAvailable)
                return false;
            s = s[2..];
        }
        if (s.Length != 17)
            return false;
        ulong value = 0;
        for (var i = 0; i < 6; i++)
        {
            if (i > 0 && s[(3 * i) - 1] != ':')
                return false;
            if (
                !byte.TryParse(
                    s.Slice(3 * i, 2),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out byte b
                )
            )
                return false;
            value = (value << 8) | b;
        }
        result = new BleAddress(type, (UInt48)value);
        return true;
    }

    /// <inheritdoc cref="Parse(ReadOnlySpan{char},System.IFormatProvider?)"/>
    public static BleAddress Parse(string s, IFormatProvider? provider) => Parse((ReadOnlySpan<char>)s, provider);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char},System.IFormatProvider?,out BleAddress?)"/>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleAddress? result
    )
    {
        if (s is not null)
            return TryParse((ReadOnlySpan<char>)s, provider, out result);
        result = default;
        return false;
    }

    /// <summary> Zero BleAddress with type <see cref="BleAddressType.NotAvailable"/> </summary>
    public static BleAddress NotAvailable { get; } = new(BleAddressType.NotAvailable, UInt48.Zero);

    /// <summary> The address as six hex bytes separated by colons, most significant first, e.g. AA:BB:CC:DD:EE:FF </summary>
    /// <remarks> <see cref="Parse(string,IFormatProvider?)"/> reads it back without the type; the format "T" keeps it </remarks>
    /// <returns> The formatted address </returns>
    public override string ToString() =>
        string.Join(':', Value.ToString("X12", CultureInfo.InvariantCulture).Chunk(2).Select(pair => new string(pair)));

    /// <summary> The address in a format: null or empty as <see cref="ToString()"/>; "T" with its type in front, P: for public and R: for random addresses </summary>
    /// <remarks> Only public and random addresses get a type; <see cref="Parse(string,IFormatProvider?)"/> reads both forms back </remarks>
    /// <param name="format"> Null, empty or "T" </param>
    /// <param name="formatProvider"> Ignored </param>
    /// <returns> The formatted address </returns>
    /// <exception cref="FormatException"> Thrown for any other format </exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        format switch
        {
            null or "" => ToString(),
            "T" => Type switch
            {
                BleAddressType.Public => $"P:{this}",
                BleAddressType.Random => $"R:{this}",
                _ => ToString(),
            },
            _ => throw new FormatException($"The format '{format}' is not supported; use \"T\" or none"),
        };

    /// <inheritdoc />
    public bool Equals(BleAddress? other)
    {
        if (other is null)
            return false;
        return Type == other.Type && Value == other.Value;
    }

    /// <inheritdoc />
    /// <remarks> Ignores the type of the address </remarks>
    public bool Equals(UInt48 other) => Value == other;

    /// <inheritdoc />
    /// <remarks> Ignores the type of the address </remarks>
    public bool Equals(ulong other) => Value == other;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine((int)Type, Value);

    /// <summary> Creates a new random address of kind <see cref="BleRandomAddressKind.Static"/> </summary>
    /// <returns> The new address </returns>
    public static BleAddress NewRandomStaticAddress()
    {
        Span<byte> buffer = stackalloc byte[6];
        RandomNumberGenerator.Fill(buffer);
        buffer[5] = (byte)(buffer[5] | 0b11000000);
        var value = new UInt48(buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5]);
        return new BleAddress(BleAddressType.Random, value);
    }

    /// <summary> Create a BleAddress of type <see cref="BleAddressType.Random"/> from a uint48 </summary>
    /// <param name="value"> The value to base the address on </param>
    /// <returns> The random BleAddress. Its <see cref="RandomKind"/> is inferred from the value </returns>
    public static BleAddress CreateRandomAddress(UInt48 value) => new(BleAddressType.Random, value);
}
