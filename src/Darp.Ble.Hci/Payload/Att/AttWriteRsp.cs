using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_WRITE_RSP PDU is sent in reply to a valid ATT_WRITE_REQ PDU and acknowledges that the attribute has been successfully written </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-1c620bba-1248-f7dd-9a8d-df41506670e7"/>
[BinaryObject]
public readonly partial record struct AttWriteRsp : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_RSP;

    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
}
