using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
[BinaryObject]
public readonly partial record struct AttReadRsp() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_RSP;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> A list of Attribute Data </summary>
    public required ReadOnlyMemory<byte> AttributeValue { get; init; }
}
