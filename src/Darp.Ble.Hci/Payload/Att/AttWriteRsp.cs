using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_WRITE_RSP PDU is sent in reply to a valid ATT_WRITE_REQ PDU and acknowledges that the attribute has been successfully written </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-1c620bba-1248-f7dd-9a8d-df41506670e7"/>
public readonly record struct AttWriteRsp : IAttPdu, IDecodable<AttWriteRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_RSP;

    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }

    /// <inheritdoc />
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