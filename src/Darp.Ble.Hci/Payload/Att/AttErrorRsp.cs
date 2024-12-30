using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_ERROR_RSP PDU is used to state that a given request cannot be performed, and to provide the reason </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9f07d82d-da59-ca27-4ee2-b404bbba3f54"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[BinaryObject]
public readonly partial record struct AttErrorRsp : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_ERROR_RSP;
    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }

    /// <summary> The request that generated this ATT_ERROR_RSP PDU </summary>
    public required AttOpCode RequestOpCode { get; init; }
    /// <summary> The attribute handle that generated this ATT_ERROR_RSP PDU </summary>
    public required ushort Handle { get; init; }
    /// <summary> The reason why the request has generated an ATT_ERROR_RSP PDU </summary>
    public required AttErrorCode ErrorCode { get; init; }
}