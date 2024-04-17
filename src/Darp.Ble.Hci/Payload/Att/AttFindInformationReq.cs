using System.Buffers.Binary;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.1 ATT_READ_BY_TYPE_REQ
/// </summary>
public readonly struct AttFindInformationReq : IAttPdu, IEncodable
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort StartingHandle { get; init; }
    public required ushort EndingHandle { get; init; }

    public int Length => 5;
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], StartingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[3..], EndingHandle);
        return true;
    }
}