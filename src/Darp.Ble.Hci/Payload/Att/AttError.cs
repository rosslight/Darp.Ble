using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

public enum AttErrorCode
{
    InvalidHandle = 0x01,
    ReadNotPermittedError = 0x02,
    WriteNotPermittedError = 0x03,
    InvalidPduError = 0x04,
    InsufficientAuthenticationError = 0x05,
    RequestNotSupportedError = 0x06,
    InvalidOffsetError = 0x07,
    InsufficientAuthorizationError = 0x08,
    PrepareQueueFullError = 0x09,
    AttributeNotFoundError = 0x0A,
    AttributeNotLongError = 0x0B,
    InsufficientEncryptionKeySizeError = 0x0C,
    InvalidAttributeLengthError = 0x0D,
    UnlikelyErrorError = 0x0E,
    InsufficientEncryptionError = 0x0F,
    UnsupportedGroupTypeError = 0x10,
    InsufficientResourcesError = 0x11,
}

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.1.1 ATT_ERROR_RSP
/// </summary>
public readonly struct AttErrorRsp : IAttPdu, IDecodable<AttErrorRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_ERROR_RSP;
    public required AttOpCode OpCode { get; init; }
    public required AttOpCode RequestOpCode { get; init; }
    /// <summary> The attribute handle that generated this ATT_ERROR_RSP PDU </summary>
    public required ushort Handle { get; init; }
    public required AttErrorCode ErrorCode { get; init; }
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
            ErrorCode = (AttErrorCode)span[4]
        };
        return true;
    }
}