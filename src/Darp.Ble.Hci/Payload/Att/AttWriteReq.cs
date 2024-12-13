using System.Buffers.Binary;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_WRITE_REQ PDU is used to request the server to write the value of an attribute and acknowledge that this has been achieved in an ATT_WRITE_RSP PDU </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-1c620bba-1248-f7dd-9a8d-df41506670e7"/>
[BinaryObject]
public readonly partial record struct AttWriteReq : IAttPdu, IEncodable
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;
    /// <summary> The handle of the attribute to be written </summary>
    public required ushort Handle { get; init; }
    /// <summary> The value to be written to the attribute </summary>
    public required ReadOnlyMemory<byte> Value { get; init; }

    /// <inheritdoc />
    public int Length => 3 + Value.Length;

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], Handle);
        return Value.Span.TryCopyTo(destination[3..]);
    }
}