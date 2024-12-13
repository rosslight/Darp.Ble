using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_FIND_INFORMATION_REQ PDU is used to obtain the mapping of attribute handles with their associated types. This allows a client to discover the list of attributes and their types on a server </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-06819664-297a-8234-c748-a326bbfab199"/>
[BinaryObject]
public readonly partial record struct AttFindInformationReq : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;
    /// <summary> First requested handle number </summary>
    public required ushort StartingHandle { get; init; }
    /// <summary> Last requested handle number </summary>
    public required ushort EndingHandle { get; init; }
}