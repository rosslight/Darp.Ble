using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_EXCHANGE_MTU_RSP PDU is sent in reply to a received ATT_EXCHANGE_MTU_REQ PDU </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3528b2c0-267c-82e7-6a13-a0973fc2680e"/>
[BinaryObject]
public readonly partial record struct AttExchangeMtuRsp : IAttPdu
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
        bytesDecoded = 0;
        if (source.Length < 3) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        result = new AttExchangeMtuRsp
        {
            OpCode = opCode,
            ServerRxMtu = BinaryPrimitives.ReadUInt16LittleEndian(span[1..]),
        };
        bytesDecoded = 3;
        return true;
    }
}