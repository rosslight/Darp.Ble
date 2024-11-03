using System.Buffers.Binary;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_EXCHANGE_MTU_REQ PDU is used by the client to inform the server of the client’s maximum receive MTU size and request the server to respond with its maximum receive MTU size </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3528b2c0-267c-82e7-6a13-a0973fc2680e"/>
public readonly record struct AttExchangeMtuReq : IAttPdu, IEncodable
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_EXCHANGE_MTU_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;
    /// <summary> Client receive MTU size </summary>
    public required ushort ClientRxMtu { get; init; }

    /// <inheritdoc />
    public int Length => 3;

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], ClientRxMtu);
        return true;
    }
}