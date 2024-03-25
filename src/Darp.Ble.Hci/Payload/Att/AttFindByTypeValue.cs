using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

public readonly struct AttFindByTypeValueReq : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_BY_TYPE_VALUE_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort StartingHandle { get; init; }
    public required ushort EndingHandle { get; init; }
    public required ushort AttributeType { get; init; }
    public required byte[] AttributeValue { get; init; }

    public int Length => 7 + AttributeValue.Length;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], StartingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[3..], EndingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[5..], AttributeType);
        return AttributeValue.AsSpan().TryCopyTo(destination[7..]);
    }
}

public readonly record struct AttFindByTypeHandlesInformation(ushort Handle, ushort EndGroup);

public readonly struct AttFindByTypeValueRsp : IAttPdu, IDecodable<AttFindByTypeValueRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP;
    public required AttOpCode OpCode { get; init; }
    public required AttFindByTypeHandlesInformation[] HandlesInformationList { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttFindByTypeValueRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 5) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        if ((source.Length - 1) % 2 != 0) return false;
        result = new AttFindByTypeValueRsp
        {
            OpCode = opCode,
            HandlesInformationList = MemoryMarshal.Cast<byte, AttFindByTypeHandlesInformation>(span[1..]).ToArray(),
        };
        return true;
    }
}