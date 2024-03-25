using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.1 ATT_READ_BY_TYPE_REQ
/// </summary>
public readonly struct AttReadByTypeReq<TAttributeType> : IAttPdu, IEncodable
    where TAttributeType : unmanaged
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_REQ;
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

public readonly record struct AttTypeData(ushort Handle, byte[] Value);

/// <summary>
/// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part F, 3.4.4.2 ATT_READ_BY_TYPE_RSP
/// </summary>
public readonly struct AttReadByTypeRsp : IAttPdu, IDecodable<AttReadByTypeRsp>
{
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_TYPE_RSP;
    public required AttOpCode OpCode { get; init; }
    public required byte Length { get; init; }
    public required AttTypeData[] AttributeDataList { get; init; }
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttReadByTypeRsp result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = source.Length;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        var opCode = (AttOpCode)span[0];
        if (opCode != ExpectedOpCode) return false;
        byte length = span[1];
        if (length < 2) return false;
        if ((source.Length - 2) % length != 0) return false;
        int numberOfAttributes = (source.Length - 2) / length;
        var attributeDataList = new AttTypeData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * length;
            attributeDataList[i] = new AttTypeData(
                BinaryPrimitives.ReadUInt16LittleEndian(span[attStart..]),
                span[(attStart + 2)..(attStart + length)].ToArray());
        }
        result = new AttReadByTypeRsp
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = attributeDataList
        };
        return true;
    }
}