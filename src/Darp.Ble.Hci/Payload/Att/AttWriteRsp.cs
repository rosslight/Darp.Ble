using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.5.2 ATT_WRITE_RSP
/// </summary>
public readonly struct AttWriteRsp : IAttPdu, IDecodable<AttWriteRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_RSP;
    public required AttOpCode OpCode { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttWriteRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length == 0) return false;
        var opCode = (AttOpCode)source.Span[0];
        if (opCode != ExpectedOpCode) return false;
        result = new AttWriteRsp
        {
            OpCode = opCode,
        };
        return true;
    }
}