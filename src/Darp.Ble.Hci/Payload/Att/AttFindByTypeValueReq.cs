using System.Buffers.Binary;

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