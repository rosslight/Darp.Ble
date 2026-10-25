using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BLOB_RSP PDU is sent in reply to a received ATT_READ_BLOB_REQ PDU and contains part of the value of the attribute that has been read </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
[BinaryObject]
public readonly partial record struct AttReadBlobRsp() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BLOB_RSP;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> Part of the value of the attribute, starting at the requested offset </summary>
    public required ReadOnlyMemory<byte> PartAttributeValue { get; init; }
}
