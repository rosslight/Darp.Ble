using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_REQ PDU is used to obtain the values of attributes where the attribute type is known but the handle is not known. </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttReadByTypeReq<TAttributeType> : IAttPdu, IEncodable
    where TAttributeType : unmanaged
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_REQ;

    /// <inheritdoc />
    public AttOpCode OpCode => ExpectedOpCode;
    /// <summary> First requested handle number </summary>
    public required ushort StartingHandle { get; init; }
    /// <summary> Last requested handle number </summary>
    public required ushort EndingHandle { get; init; }
    /// <summary> 2 or 16 octet UUID </summary>
    public required TAttributeType AttributeType { get; init; }

    /// <inheritdoc />
    public int Length => 5 + Marshal.SizeOf<TAttributeType>();

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)OpCode;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], StartingHandle);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[3..], EndingHandle);
        Span<TAttributeType> attributeTypeSpan = stackalloc TAttributeType[1];
        attributeTypeSpan[0] = AttributeType;
        return MemoryMarshal.Cast<TAttributeType, byte>(attributeTypeSpan).TryCopyTo(destination[5..]);
    }
}