using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.5.1 ATT_WRITE_REQ
/// </summary>
public readonly struct AttWriteReq : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort Handle { get; init; }
    public required byte[] Value { get; init; }

    public int Length => 3 + Value.Length;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], Handle);
        return Value.AsSpan().TryCopyTo(destination[3..]);
    }
}

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.5.2 ATT_WRITE_RSP
/// </summary>
public readonly struct AttWriteRsp : IAttPdu, IDecodable<AttWriteRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_RSP;
    public required AttOpCode OpCode { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttWriteRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length == 0) return false;
        var opCode = (AttOpCode)source.Span[0];
        if (opCode != ExpectedOpCode) return false;
        result = new AttWriteRsp
        {
            OpCode = opCode,
        };
        return true;
    }
}

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.5.3 ATT_WRITE_CMD
/// </summary>
public readonly struct AttWriteCmd : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_CMD;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort Handle { get; init; }
    public required byte[] Value { get; init; }

    public int Length => 3 + Value.Length;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], Handle);
        return Value.AsSpan().TryCopyTo(destination[3..]);
    }
}