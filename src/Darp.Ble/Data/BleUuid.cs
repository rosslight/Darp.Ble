using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Darp.Ble.Data;

/// <summary> The Ble Uuid </summary>
public sealed record BleUuid
    : ISpanParsable<BleUuid>,
        ISpanFormattable,
        IUtf8SpanFormattable,
        IEquatable<Guid?>,
        IEquatable<uint?>,
        IEquatable<ushort?>
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

    private static BleUuidType InferType(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        if (
            !(
                bytes[4] is 0x00
                && bytes[5] is 0x00
                && bytes[6] is 0x00
                && bytes[7] is 0x10
                && bytes[8] is 0x80
                && bytes[9] is 0x00
                && bytes[10] is 0x00
                && bytes[11] is 0x80
                && bytes[12] is 0x5F
                && bytes[13] is 0x9B
                && bytes[14] is 0x34
                && bytes[15] is 0xFB
            )
        )
        {
            return BleUuidType.Uuid128;
        }
        if (bytes[2] is 0x00 && bytes[3] is 0x00)
            return BleUuidType.Uuid16;
        return BleUuidType.Uuid32;
    }

    private static Guid CreateGuid(uint a) => new(a, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB);

    /// <summary> Initializes a BleUuid from a readonly span of bytes </summary>
    /// <param name="source"> The source to be decoded. </param>
    /// <returns> The bleUuid with type depending on length of source </returns>
    /// <exception cref="ArgumentOutOfRangeException"> The source is not of length 2,4,16 </exception>
    [SetsRequiredMembers]
    public BleUuid(ReadOnlySpan<byte> source)
    {
        (BleUuidType Type, Guid Guid) tuple = source.Length switch
        {
            2 => (BleUuidType.Uuid16, CreateGuid(BinaryPrimitives.ReadUInt16LittleEndian(source))),
            4 => (BleUuidType.Uuid32, CreateGuid(BinaryPrimitives.ReadUInt32LittleEndian(source))),
            16 => (BleUuidType.Uuid128, new Guid(source)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                $"Provided invalid number of bytes for uuid: {source.Length}"
            ),
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
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleUuid? result
    )
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
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out BleUuid? result
    )
    {
        if (s is not null)
            return TryParse((ReadOnlySpan<char>)s, provider, out result);
        result = null;
        return false;
    }

    /// <inheritdoc />
    /// <remarks> Infers type from the given Guid </remarks>
    public bool Equals(Guid? other)
    {
        return Value == other && Type == InferType(other.Value);
    }

    /// <inheritdoc />
    /// <remarks> Expects guid to be <see cref="BleUuidType.Uuid32"/> </remarks>
    public bool Equals(uint? other) =>
        other is not null && Type == BleUuidType.Uuid32 && Value == CreateGuid(other.Value);

    /// <inheritdoc />
    /// <remarks> Expects guid to be <see cref="BleUuidType.Uuid16"/> </remarks>
    public bool Equals(ushort? other) =>
        other is not null && Type == BleUuidType.Uuid16 && Value == CreateGuid(other.Value);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Type, Value);

    /// <inheritdoc />
    public override string ToString() =>
        Type switch
        {
            BleUuidType.Uuid16 => $"{AsUShort():X4}",
            BleUuidType.Uuid32 => $"{AsUInt():X8}",
            _ => Value.ToString(),
        };

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Type switch
        {
            BleUuidType.Uuid16 => $"{AsUShort():X4}",
            BleUuidType.Uuid32 => $"{AsUInt():X8}",
            _ => Value.ToString(format, formatProvider),
        };

    /// <inheritdoc cref="Guid.TryFormat(System.Span{char},out int,System.ReadOnlySpan{char})"/>/>
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default
    ) =>
        Type switch
        {
            BleUuidType.Uuid16 => AsUShort()
                .TryFormat(destination, out charsWritten, "X4", CultureInfo.InvariantCulture),
            BleUuidType.Uuid32 => AsUInt().TryFormat(destination, out charsWritten, "X8", CultureInfo.InvariantCulture),
            _ => Value.TryFormat(destination, out charsWritten, format),
        };

    /// <inheritdoc cref="Guid.TryFormat(System.Span{byte},out int,System.ReadOnlySpan{char})"/>/>
    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        [StringSyntax("GuidFormat")] ReadOnlySpan<char> format = default
    ) =>
        Type switch
        {
            BleUuidType.Uuid16 => AsUShort()
                .TryFormat(utf8Destination, out bytesWritten, "X4", CultureInfo.InvariantCulture),
            BleUuidType.Uuid32 => AsUInt()
                .TryFormat(utf8Destination, out bytesWritten, "X8", CultureInfo.InvariantCulture),
            _ => Value.TryFormat(utf8Destination, out bytesWritten, format),
        };

    bool ISpanFormattable.TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        return TryFormat(destination, out charsWritten, format);
    }

    bool IUtf8SpanFormattable.TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        return TryFormat(utf8Destination, out bytesWritten, format);
    }

    private ushort AsUShort()
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes[..2]);
    }

    private uint AsUInt()
    {
        Span<byte> bytes = stackalloc byte[16];
        Value.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes[..4]);
    }

    /// <summary> Write the ble uuid to a span </summary>
    /// <param name="destination"> The span to be written to </param>
    /// <returns> True if the uuid is successfully written to the specified span; False otherwise </returns>
    public bool TryWriteBytes(Span<byte> destination)
    {
        switch (Type)
        {
            case BleUuidType.Uuid16:
            {
                Span<byte> bytes = stackalloc byte[16];
                Value.TryWriteBytes(bytes);
                return bytes[..2].TryCopyTo(destination);
            }
            case BleUuidType.Uuid32:
            {
                Span<byte> bytes = stackalloc byte[16];
                Value.TryWriteBytes(bytes);
                return bytes[..4].TryCopyTo(destination);
            }
            case BleUuidType.Uuid128:
                return Value.TryWriteBytes(destination);
            default:
                return false;
        }
    }

    /// <summary> Write the ble uuid to a byte array </summary>
    /// <returns> The byte array. Length depends on the <see cref="Type"/> </returns>
    public byte[] ToByteArray()
    {
        var buffer = new byte[(int)Type];
        if (!TryWriteBytes(buffer))
            throw new Exception("Ble uuid is corrupt. Could not write bytes");
        return buffer;
    }

    /// <summary> Creates a new BleUuid from a 16-bit integer </summary>
    /// <param name="value"> The 16-bit uuid </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid16"/> </returns>
    public static implicit operator BleUuid(ushort value) => FromUInt16(value);

    /// <summary> Creates a new BleUuid from a 16-bit integer </summary>
    /// <param name="value"> The 16-bit uuid </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid16"/> </returns>
    public static BleUuid FromUInt16(ushort value) => new(BleUuidType.Uuid16, CreateGuid(value));

    /// <summary> Creates a new BleUuid from a 32-bit integer </summary>
    /// <param name="value"> The 16-bit uuid </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid32"/> </returns>
    public static BleUuid FromUInt32(uint value) => new(BleUuidType.Uuid32, CreateGuid(value));

    /// <summary> Creates a newBleUuid from a guid </summary>
    /// <param name="value"> The uuid </param>
    /// <param name="inferType"> If true, the <see cref="BleUuidType"/> will be inferred from the given <paramref name="value"/>; <see cref="BleUuidType.Uuid128"/> otherwise </param>
    /// <returns> The bleUuid with type <see cref="BleUuidType.Uuid128"/> or inferred type </returns>
    public static BleUuid FromGuid(Guid value, bool inferType = false) =>
        new(inferType ? InferType(value) : BleUuidType.Uuid128, value);
}
