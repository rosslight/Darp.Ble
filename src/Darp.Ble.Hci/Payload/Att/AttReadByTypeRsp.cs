using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.2 ATT_READ_BY_TYPE_RSP
/// </summary>
public readonly struct AttReadByTypeRsp : IAttPdu, IDecodable<AttReadByTypeRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_RSP;
    public required AttOpCode OpCode { get; init; }
    public required byte Length { get; init; }
    public required AttTypeData[] AttributeDataList { get; init; }
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
        var attributeDataList = new AttTypeData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * length;
            attributeDataList[i] = new AttTypeData(
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