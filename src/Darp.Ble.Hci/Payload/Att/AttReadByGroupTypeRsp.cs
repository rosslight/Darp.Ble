using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <typeparam name="TAttributeValue"> The type of the attribute </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
[BinaryObject]
public readonly partial record struct AttReadByGroupTypeRsp<TAttributeValue> : IAttPdu
    where TAttributeValue : unmanaged
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP;

    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }
    /// <summary> The size of each attribute handle-value pair </summary>
    public required byte Length { get; init; }
    /// <summary> A list of Attribute Data </summary>
    [BinaryElementCount(nameof(Length))]
    public required AttGroupTypeData<TAttributeValue>[] AttributeDataList { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> result, out int bytesDecoded)
    {
        result = default;
        bytesDecoded = 0;
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
        bytesDecoded = source.Length;
        return true;
    }
}