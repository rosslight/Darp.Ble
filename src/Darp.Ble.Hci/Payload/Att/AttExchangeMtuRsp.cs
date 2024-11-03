using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_EXCHANGE_MTU_RSP PDU is sent in reply to a received ATT_EXCHANGE_MTU_REQ PDU </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3528b2c0-267c-82e7-6a13-a0973fc2680e"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttExchangeMtuRsp : IAttPdu, IDecodable<AttExchangeMtuRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_EXCHANGE_MTU_RSP;

    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
    /// <summary> ATT Server receive MTU size </summary>
    public required ushort ServerRxMtu { get; init; }

    /// <inheritdoc />
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