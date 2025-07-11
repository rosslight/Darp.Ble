using System.Buffers.Binary;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
public readonly partial record struct AttReadByTypeRsp() : IAttPdu, IBinaryObject<AttReadByTypeRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_RSP;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> The size of each attribute handle-value pair </summary>
    public required byte Length { get; init; }

    /// <summary> A list of Attribute Data </summary>
    [BinaryElementCount(nameof(Length))]
    public required AttReadByTypeData[] AttributeDataList { get; init; }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttReadByTypeRsp value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttReadByTypeRsp value, out int bytesRead)
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 6)
        {
            return false;
        }
        var opCode = (AttOpCode)source[0];
        if (opCode != ExpectedOpCode)
        {
            return false;
        }
        byte length = source[1];
        if (length < 2)
        {
            return false;
        }

        if ((source.Length - 2) % length != 0)
        {
            return false;
        }
        int numberOfAttributes = (source.Length - 2) / length;
        var attributeDataList = new AttReadByTypeData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i++)
        {
            int attStart = 2 + i * length;
            attributeDataList[i] = new AttReadByTypeData(
                BinaryPrimitives.ReadUInt16LittleEndian(source[attStart..]),
                source.Slice(attStart + 2, length - 2).ToArray()
            );
        }
        value = new AttReadByTypeRsp
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = attributeDataList,
        };
        bytesRead = source.Length;
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByTypeRsp value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByTypeRsp value, out int bytesRead)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public int GetByteCount() => 2 + (AttributeDataList.Length * Length);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination) => TryWriteLittleEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        int attributeDataListLength = AttributeDataList.Length * Length;
        if (destination.Length < 2 + attributeDataListLength)
            return false;
        destination[0] = (byte)OpCode;
        destination[1] = Length;
        bytesWritten += 2;
        foreach ((ushort handle, ReadOnlyMemory<byte> value) in AttributeDataList)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination[bytesWritten..], handle);
            value.Span.CopyTo(destination[(bytesWritten + 2)..]);
            bytesWritten += 2 + value.Length;
        }
        return true;
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination) => TryWriteBigEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotSupportedException();
}
