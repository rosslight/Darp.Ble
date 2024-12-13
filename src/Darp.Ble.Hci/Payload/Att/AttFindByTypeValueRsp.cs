using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_FIND_BY_TYPE_VALUE_RSP PDU is sent in reply to a received ATT_FIND_BY_TYPE_VALUE_REQ PDU and contains information about this server </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-06819664-297a-8234-c748-a326bbfab199"/>
[BinaryObject]
public readonly partial record struct AttFindByTypeValueRsp : IAttPdu
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP;
    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
    /// <summary> A list of 1 or more Handle Information </summary>
    public required AttFindByTypeHandlesInformation[] HandlesInformationList { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttFindByTypeValueRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = 0;
        if (source.Length < 5) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        if ((source.Length - 1) % 2 != 0) return false;
        result = new AttFindByTypeValueRsp
        {
            OpCode = opCode,
            HandlesInformationList = MemoryMarshal.Cast<byte, AttFindByTypeHandlesInformation>(span[1..]).ToArray(),
        };
        bytesDecoded = source.Length;
        return true;
    }
}