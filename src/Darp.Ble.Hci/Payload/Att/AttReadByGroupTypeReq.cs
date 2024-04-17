using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F 3.4.4.9 ATT_READ_BY_GROUP_TYPE_REQ </summary>
/// <typeparam name="TAttributeType">The type of the attribute</typeparam>
public readonly struct AttReadByGroupTypeReq<TAttributeType> : IAttPdu, IEncodable
    where TAttributeType : unmanaged
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ;
    public AttOpCode OpCode => ExpectedOpCode;
    public required ushort StartingHandle { get; init; }
    public required ushort EndingHandle { get; init; }
    public required TAttributeType AttributeType { get; init; }

    public int Length => 5 + Marshal.SizeOf<TAttributeType>();
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