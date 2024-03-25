using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

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

public readonly record struct AttGroupTypeData<TAttributeValue>(ushort Handle, ushort EndGroup, TAttributeValue Value)
    where TAttributeValue : unmanaged;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F 3.4.4.10 ATT_READ_BY_GROUP_TYPE_RSP </summary>
/// <typeparam name="TAttributeValue">The type of the attribute</typeparam>
public readonly struct AttReadByGroupTypeRsp<TAttributeValue> : IAttPdu, IDecodable<AttReadByGroupTypeRsp<TAttributeValue>>
    where TAttributeValue : unmanaged
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP;
    public required AttOpCode OpCode { get; init; }
    public required byte Length { get; init; }
    public required AttGroupTypeData<TAttributeValue>[] AttributeDataList { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        byte length = span[1];
        if (length != Unsafe.SizeOf<AttGroupTypeData<TAttributeValue>>()) return false;
        if ((source.Length - 2) % length != 0) return false;
        result = new AttReadByGroupTypeRsp<TAttributeValue>
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = MemoryMarshal.Cast<byte, AttGroupTypeData<TAttributeValue>>(span[2..]).ToArray(),
        };
        return true;
    }
}