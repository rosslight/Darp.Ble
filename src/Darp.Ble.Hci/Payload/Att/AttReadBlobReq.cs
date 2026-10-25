using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BLOB_REQ PDU is used to request the server to read part of the value of an attribute at a given offset and return a specific part of the value in an ATT_READ_BLOB_RSP PDU </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
[BinaryObject]
public readonly partial record struct AttReadBlobReq() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BLOB_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode { get; init; } = ExpectedOpCode;

    /// <summary> The handle of the attribute to be read </summary>
    public required ushort AttributeHandle { get; init; }

    /// <summary> The offset of the first octet to be read </summary>
    public required ushort ValueOffset { get; init; }
}
