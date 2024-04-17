using System.Buffers.Binary;

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