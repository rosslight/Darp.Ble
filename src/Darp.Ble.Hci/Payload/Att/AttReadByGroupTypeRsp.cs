using System.Diagnostics;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
public readonly record struct AttReadByGroupTypeRsp() : IAttPdu, IBinaryObject<AttReadByGroupTypeRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> The size of each attribute handle-value pair </summary>
    public required byte Length { get; init; }

    /// <summary> A list of Attribute Data </summary>
    public required AttGroupTypeData[] AttributeDataList { get; init; }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp value) =>
        TryReadLittleEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadLittleEndian(
        ReadOnlySpan<byte> source,
        out AttReadByGroupTypeRsp value,
        out int bytesRead
    )
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 6)
            return false;
        var opCode = (AttOpCode)source[0];
        if (opCode != ExpectedOpCode)
            return false;
        byte length = source[1];
        if ((source.Length - 2) % length != 0)
            return false;
        int numberOfAttributeData = (source.Length - 2) / length;
        var attributeDataList = new AttGroupTypeData[numberOfAttributeData];
        for (int i = 0; i < numberOfAttributeData; i++)
        {
            ReadOnlySpan<byte> slice = source.Slice(2 + (i * length), length);
            if (!AttGroupTypeData.TryReadLittleEndian(slice, out AttGroupTypeData data))
                return false;
            attributeDataList[i] = data;
        }
        value = new AttReadByGroupTypeRsp
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = attributeDataList,
        };
        bytesRead = source.Length;
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp value) =>
        TryReadBigEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadBigEndian(
        ReadOnlySpan<byte> source,
        out AttReadByGroupTypeRsp value,
        out int bytesRead
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public int GetByteCount() => 2 + (AttributeDataList.Length * Length);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination) => TryWriteLittleEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        Debug.Assert(AttributeDataList.Length == 0 || Length == AttributeDataList[0].GetByteCount());

        bytesWritten = 0;
        if (destination.Length < 2)
            return false;
        destination[0] = (byte)OpCode;
        destination[1] = Length;
        bytesWritten += 2;
        Span<byte> slice = destination[2..];
        foreach (AttGroupTypeData attributeData in AttributeDataList)
        {
            if (!attributeData.TryWriteLittleEndian(slice, out int dataWritten))
                return false;
            slice = slice[dataWritten..];
            bytesWritten += dataWritten;
        }
        return true;
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination) => TryWriteBigEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotSupportedException();
}
