using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_FIND_BY_TYPE_VALUE_REQ PDU is used to obtain the handles of attributes that have a 16-bit UUID attribute type and attribute value </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-06819664-297a-8234-c748-a326bbfab199"/>
[BinaryObject]
public readonly partial record struct AttFindByTypeValueReq() : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_BY_TYPE_VALUE_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode { get; private init; } = ExpectedOpCode;

    /// <summary> First requested handle number </summary>
    public required ushort StartingHandle { get; init; }

    /// <summary> Last requested handle number </summary>
    public required ushort EndingHandle { get; init; }

    /// <summary> 2 octet UUID to find </summary>
    public required ushort AttributeType { get; init; }

    /// <summary> Attribute value to find </summary>
    public required ReadOnlyMemory<byte> AttributeValue { get; init; }
}
