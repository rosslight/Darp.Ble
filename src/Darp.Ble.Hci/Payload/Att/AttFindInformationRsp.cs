using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.2 ATT_READ_BY_TYPE_RSP
/// </summary>
public readonly struct AttFindInformationRsp : IAttPdu, IDecodable<AttFindInformationRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_RSP;
    public required AttOpCode OpCode { get; init; }
    public required AttInformationFormat Format { get; init; }
    public required AttInformationData[] AttributeDataList { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttFindInformationRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        var format = (AttInformationFormat)span[1];
        int length = 2 + format switch
        {
            AttInformationFormat.HandleAnd16BitUuid => 2,
            AttInformationFormat.HandleAnd128BitUuid => 16,
            _ => -1,
        };
        if (length < 4) return false;
        if ((source.Length - 2) % length != 0) return false;
        int numberOfAttributes = (source.Length - 2) / (2 + 2);
        var attributeDataList = new AttInformationData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * length;
            attributeDataList[i] = new AttInformationData(
                BinaryPrimitives.ReadUInt16LittleEndian(span[attStart..]),
                BinaryPrimitives.ReadUInt16LittleEndian(span[(attStart + 2)..]));
        }
        result = new AttFindInformationRsp
        {
            OpCode = opCode,
            Format = format,
            AttributeDataList = attributeDataList,
        };
        return true;
    }
}