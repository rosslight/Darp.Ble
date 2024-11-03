using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
public readonly record struct AttReadByTypeRsp : IAttPdu, IDecodable<AttReadByTypeRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_RSP;

    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
    /// <summary> The size of each attribute handle-value pair </summary>
    public required byte Length { get; init; }
    /// <summary> A list of Attribute Data </summary>
    public required AttReadByTypeData[] AttributeDataList { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttReadByTypeRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        byte length = span[1];
        if (length < 2) return false;
        if ((source.Length - 2) % length != 0) return false;
        int numberOfAttributes = (source.Length - 2) / length;
        var attributeDataList = new AttReadByTypeData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * length;
            attributeDataList[i] = new AttReadByTypeData(
                BinaryPrimitives.ReadUInt16LittleEndian(span[attStart..]),
                span[(attStart + 2)..(attStart + length)].ToArray());
        }
        result = new AttReadByTypeRsp
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = attributeDataList,
        };
        return true;
    }
}