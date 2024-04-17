using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

public readonly struct AttExchangeMtuRsp : IAttPdu, IDecodable<AttExchangeMtuRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_EXCHANGE_MTU_RSP;
    public required AttOpCode OpCode { get; init; }
    public required ushort ServerRxMtu { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttExchangeMtuRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 3) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        result = new AttExchangeMtuRsp
        {
            OpCode = opCode,
            ServerRxMtu = BinaryPrimitives.ReadUInt16BigEndian(span[1..]),
        };
        return true;
    }
}