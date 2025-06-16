using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_REQ PDU is used to obtain the values of attributes where the attribute type is known but the handle is not known </summary>
/// <typeparam name="TAttributeType">The type of the attribute</typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
public readonly partial record struct AttReadByGroupTypeReq<TAttributeType>
    : IAttPdu,
        IBinaryObject<AttReadByGroupTypeReq<TAttributeType>>
    where TAttributeType : unmanaged
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ;

    /// <inheritdoc />
    public int GetByteCount() => 5 + Marshal.SizeOf<TAttributeType>();

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;

    /// <summary> First requested handle number </summary>
    public required ushort StartingHandle { get; init; }

    /// <summary> Last requested handle number </summary>
    public required ushort EndingHandle { get; init; }

    /// <summary> 2 or 16 octet UUID </summary>
    public required TAttributeType AttributeGroupType { get; init; }

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination)
    {
        return TryWriteLittleEndian(destination, out _);
    }

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if (destination.Length < 6)
            return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], StartingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[3..], EndingHandle);
        Span<TAttributeType> attributeTypeSpan = stackalloc TAttributeType[1];
        attributeTypeSpan[0] = AttributeGroupType;
        bytesWritten = GetByteCount();
        return MemoryMarshal.Cast<TAttributeType, byte>(attributeTypeSpan).TryCopyTo(destination[5..]);
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination) => TryWriteBigEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotSupportedException();

    /// <inheritdoc />
    public static bool TryReadLittleEndian(
        ReadOnlySpan<byte> source,
        out AttReadByGroupTypeReq<TAttributeType> value
    ) => TryReadLittleEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadLittleEndian(
        ReadOnlySpan<byte> source,
        out AttReadByGroupTypeReq<TAttributeType> value,
        out int bytesRead
    )
    {
        bytesRead = 0;
        value = default;
        int attributeTypeLength = Marshal.SizeOf<TAttributeType>();
        if (source.Length < 5 + attributeTypeLength)
            return false;
        var opCode = (AttOpCode)source[0];
        if (opCode != ExpectedOpCode)
            return false;
        ushort startingHandle = BinaryPrimitives.ReadUInt16LittleEndian(source[1..]);
        ushort endingHandle = BinaryPrimitives.ReadUInt16LittleEndian(source[3..]);
        bytesRead += 5;
        ReadOnlySpan<TAttributeType> attributeTypeSpan = MemoryMarshal.Cast<byte, TAttributeType>(source[5..]);
        TAttributeType attributeType = attributeTypeSpan[0];
        bytesRead += attributeTypeLength;
        value = new AttReadByGroupTypeReq<TAttributeType>
        {
            StartingHandle = startingHandle,
            EndingHandle = endingHandle,
            AttributeGroupType = attributeType,
        };
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeReq<TAttributeType> value) =>
        TryReadBigEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadBigEndian(
        ReadOnlySpan<byte> source,
        out AttReadByGroupTypeReq<TAttributeType> value,
        out int bytesRead
    ) => throw new NotSupportedException();
}
