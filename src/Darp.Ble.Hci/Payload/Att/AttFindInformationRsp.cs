using System.Buffers.Binary;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_FIND_INFORMATION_RSP PDU is sent in reply to a received ATT_FIND_INFORMATION_REQ PDU and contains information about this server </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-06819664-297a-8234-c748-a326bbfab199"/>
[BinaryObject]
public readonly partial record struct AttFindInformationRsp : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_RSP;
    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }

    /// <summary> The format of the information data </summary>
    public required AttFindInformationFormat Format { get; init; }
    /// <summary> The information data whose format is determined by the Format field </summary>
    public required AttFindInformationData[] InformationData { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttFindInformationRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = 0;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        var format = (AttFindInformationFormat)span[1];
        int informationDataLength = 2 + format switch
        {
            AttFindInformationFormat.HandleAnd16BitUuid => 2,
            AttFindInformationFormat.HandleAnd128BitUuid => 16,
            _ => -1,
        };
        if (informationDataLength < 4) return false;

        if ((source.Length - 2) % informationDataLength != 0) return false;
        int numberOfAttributes = (source.Length - 2) / informationDataLength;
        var attributeDataList = new AttFindInformationData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * informationDataLength;
            attributeDataList[i] = new AttFindInformationData(
                BinaryPrimitives.ReadUInt16LittleEndian(span[attStart..]),
                source.Slice(attStart + 2, informationDataLength - 2));
        }
        result = new AttFindInformationRsp
        {
            OpCode = opCode,
            Format = format,
            InformationData = attributeDataList,
        };
        bytesDecoded = source.Length;
        return true;
    }
}