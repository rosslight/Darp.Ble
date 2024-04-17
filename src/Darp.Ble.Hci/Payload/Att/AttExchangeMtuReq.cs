using System.Buffers.Binary;

namespace Darp.Ble.Hci.Payload.Att;

public readonly struct AttExchangeMtuReq : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_EXCHANGE_MTU_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort ClientRxMtu { get; init; }

    public int Length => 3;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], ClientRxMtu);
        return true;
    }
}