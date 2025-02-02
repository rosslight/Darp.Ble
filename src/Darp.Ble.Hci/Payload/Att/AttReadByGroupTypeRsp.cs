using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_READ_BY_TYPE_RSP PDU is sent in reply to a received ATT_READ_BY_TYPE_REQ PDU and contains the handles and values of the attributes that have been read </summary>
/// <typeparam name="TAttributeValue"> The type of the attribute </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-2c2cdcd4-6173-9654-82fc-c4c7bd74fe3a"/>
public readonly record struct AttReadByGroupTypeRsp<TAttributeValue> : IAttPdu, IBinaryReadable<AttReadByGroupTypeRsp<TAttributeValue>>
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
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> value, out int bytesRead)
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 6) return false;
        var opCode = (AttOpCode)source[0];
        if (opCode != ExpectedOpCode) return false;
        byte length = source[1];
        if (length != Unsafe.SizeOf<AttGroupTypeData<TAttributeValue>>()) return false;
        if ((source.Length - 2) % length != 0) return false;
        value = new AttReadByGroupTypeRsp<TAttributeValue>
        {
            OpCode = opCode,
            Length = length,
            AttributeDataList = MemoryMarshal.Cast<byte, AttGroupTypeData<TAttributeValue>>(source[2..]).ToArray(),
        };
        bytesRead = source.Length;
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttReadByGroupTypeRsp<TAttributeValue> value, out int bytesRead)
    {
        throw new NotSupportedException();
    }
}