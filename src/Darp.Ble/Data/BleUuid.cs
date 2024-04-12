using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The Ble Uuid </summary>
public sealed record BleUuid : ISpanParsable<BleUuid>, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary> The type of the uuid </summary>
    public required BleUuidType Type { get; init; }
    /// <summary> The underlying <see cref="Guid"/> value </summary>
    public Guid Value { get; }

    /// <summary> Initializes a new ble uuid with a given type </summary>
    /// <param name="type"> The type of the uuid </param>
    /// <param name="value"> The underlying guid </param>
    [SetsRequiredMembers]
    public BleUuid(BleUuidType type, Guid value)
    {
        Type = type;
        Value = value;
    }

    /// <summary> Initializes a BleUuid from a 16 bit integer </summary>
    /// <param name="value"> The uuid </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid16"/> </returns>
    [SetsRequiredMembers]
    public BleUuid(ushort value) : this(BleUuidType.Uuid16, CreateGuid(value)) {}

    /// <summary> Initializes a BleUuid from a 32 bit integer </summary>
    /// <param name="value"> The uuid </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid32"/> </returns>
    [SetsRequiredMembers]
    public BleUuid (uint value) : this(BleUuidType.Uuid32, CreateGuid(value)) {}

    /// <summary> Initializes a BleUuid from a guid </summary>
    /// <param name="value"> The uuid </param>
    /// <param name="inferType"> If true, the <see cref="BleUuidType"/> will be inferred from the given <paramref name="value"/> </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid128"/> </returns>
    [SetsRequiredMembers]
    public BleUuid(Guid value, bool inferType = false) : this(inferType ? InferType(value) : BleUuidType.Uuid128, value) {}

    private static BleUuidType InferType(Guid value)
    {
        throw new NotImplementedException();
    }

    private static Guid CreateGuid(uint a) => new(a, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB);

    /// <summary> Initializes a BleUuid from a readonly span of bytes </summary>
    /// <param name="source"> The source to be decoded. </param>
    /// <returns> The bleUuid with type depending on length of source </returns>
    /// <exception cref="ArgumentOutOfRangeException"> The source is not of length 2,4,16 </exception>
    [SetsRequiredMembers]
    public BleUuid (ReadOnlySpan<byte> source)
    {
        (BleUuidType Type, Guid Guid) tuple = source.Length switch
        {
            2 => (BleUuidType.Uuid16, CreateGuid(BinaryPrimitives.ReadUInt16LittleEndian(source))),
            4 => (BleUuidType.Uuid32, CreateGuid(BinaryPrimitives.ReadUInt32LittleEndian(source))),
            16 => (BleUuidType.Uuid128, new Guid(source)),
            _ => throw new ArgumentOutOfRangeException(nameof(source),
                $"Provided invalid number of bytes for uuid: {source.Length}"),
        };
        Type = tuple.Type;
        Value = tuple.Guid;
    }

    /// <summary> Parses a string of suitable format and returns a ble uuid </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />. </param>
    /// <exception cref="System.FormatException"> <paramref name="s" /> is not in a recognized format. </exception>
    /// <example> 00002902-0000-1000-8000–00805f9b34fb </example>
    /// <returns> The parsed BleUuid with type <see cref="BleUuidType.Uuid128"/> or default </returns>
    public static BleUuid Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return new BleUuid(BleUuidType.Uuid128, Guid.Parse(s, provider));
    }

    /// <summary> Parses a string of suitable format and returns a ble uuid </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />. </param>
    /// <param name="result"> The parsed BleUuid with type <see cref="BleUuidType.Uuid128"/> or default </param>
    /// <example> 00002902-0000-1000-8000–00805f9b34fb </example>
    /// <returns> True if the parsing was successful </returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out BleUuid? result)
    {
        if (!Guid.TryParse(s, provider, out Guid guid))
        {
            result = null;
            return false;
        }
        result = new BleUuid(BleUuidType.Uuid128, guid);
        return true;
    }

    /// <inheritdoc cref="Parse(ReadOnlySpan{char},System.IFormatProvider?)"/>
    public static BleUuid Parse(string s, IFormatProvider? provider) => Parse((ReadOnlySpan<char>)s, provider);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char},System.IFormatProvider?,out BleUuid?)"/>
    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out BleUuid? result)
    {
        if (s is not null) return TryParse((ReadOnlySpan<char>)s, provider, out result);
        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <inheritdoc cref="Guid.TryFormat(System.Span{char},out int,System.ReadOnlySpan{char})"/>/>
    public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default)
    {
        return Value.TryFormat(destination, out charsWritten, format);
    }

    /// <inheritdoc cref="Guid.TryFormat(System.Span{byte},out int,System.ReadOnlySpan{char})"/>/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default)
    {
        return Value.TryFormat(utf8Destination, out bytesWritten, format);
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return TryFormat(destination, out charsWritten, format);
    }

    bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return TryFormat(utf8Destination, out bytesWritten, format);
    }
}