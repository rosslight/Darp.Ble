using System.Buffers.Binary;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_WRITE_CMD PDU is used to request the server to write the value of an attribute, typically into a control-point attribute </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-1c620bba-1248-f7dd-9a8d-df41506670e7"/>
public readonly record struct AttWriteCmd : IAttPdu, IEncodable
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_WRITE_CMD;

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;
    /// <summary> The handle of the attribute to be set </summary>
    public required ushort Handle { get; init; }
    /// <summary> The value of be written to the attribute </summary>
    public required byte[] Value { get; init; }

    /// <inheritdoc />
    public int Length => 3 + Value.Length;

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], Handle);
        return Value.AsSpan().TryCopyTo(destination[3..]);
    }
}