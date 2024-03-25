using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.1 ATT_READ_BY_TYPE_REQ
/// </summary>
public readonly struct AttFindInformationReq : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort StartingHandle { get; init; }
    public required ushort EndingHandle { get; init; }

    public int Length => 5;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], StartingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[3..], EndingHandle);
        return true;
    }
}

public readonly record struct AttInformationData(ushort Handle, ushort Uuid);

public enum AttInformationFormat
{
    /// <summary> A list of 1 or more handles with their 16-bit Bluetooth UUIDs </summary>
    HandleAnd16BitUuid = 0x01,
    /// <summary> A list of 1 or more handles with their 128-bit UUIDs </summary>
    HandleAnd128BitUuid = 0x02
}

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
            _ => -1
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
            AttributeDataList = attributeDataList
        };
        return true;
    }
}