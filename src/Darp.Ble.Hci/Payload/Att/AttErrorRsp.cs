using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_ERROR_RSP PDU is used to state that a given request cannot be performed, and to provide the reason </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9f07d82d-da59-ca27-4ee2-b404bbba3f54"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttErrorRsp : IAttPdu, IDecodable<AttErrorRsp>
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

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttErrorRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 5) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        result = new AttErrorRsp
        {
            OpCode = opCode,
            RequestOpCode = (AttOpCode)span[1],
            Handle = BinaryPrimitives.ReadUInt16LittleEndian(span[2..]),
            ErrorCode = (AttErrorCode)span[4],
        };
        return true;
    }
}