using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_WRITE_CMD PDU is used to request the server to write the value of an attribute, typically into a control-point attribute </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-1c620bba-1248-f7dd-9a8d-df41506670e7"/>
[BinaryObject]
public readonly partial record struct AttWriteCmd() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_CMD;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> The handle of the attribute to be set </summary>
    public required ushort Handle { get; init; }

    /// <summary> The value of be written to the attribute </summary>
    public required ReadOnlyMemory<byte> Value { get; init; }
}
