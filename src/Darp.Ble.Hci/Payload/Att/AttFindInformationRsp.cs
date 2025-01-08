using System.Buffers.Binary;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The ATT_FIND_INFORMATION_RSP PDU is sent in reply to a received ATT_FIND_INFORMATION_REQ PDU and contains information about this server </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-06819664-297a-8234-c748-a326bbfab199"/>
public readonly record struct AttFindInformationRsp : IAttPdu, IBinaryReadable<AttFindInformationRsp>
{
    /// <inheritdoc />
    public static AttOpCode ExpectedOpCode => AttOpCode.ATT_FIND_INFORMATION_RSP;
    /// <inheritdoc />
    public required AttOpCode OpCode { get; init; }

    /// <summary> The format of the information data </summary>
    public required AttFindInformationFormat Format { get; init; }
    /// <summary> The information data whose format is determined by the Format field </summary>
    public required AttFindInformationData[] InformationData { get; init; }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttFindInformationRsp value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out AttFindInformationRsp value, out int bytesRead)
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 6) return false;
        var opCode = (AttOpCode)source[0];
        if (opCode != ExpectedOpCode) return false;
        var format = (AttFindInformationFormat)source[1];
        int informationDataLength = 2 + format switch
        {
            AttFindInformationFormat.HandleAnd16BitUuid => 2,
            AttFindInformationFormat.HandleAnd128BitUuid => 16,
            _ => -1,
        };
        if (informationDataLength < 4) return false;

        if ((source.Length - 2) % informationDataLength != 0) return false;
        int numberOfAttributes = (source.Length - 2) / informationDataLength;
        var attributeDataList = new AttFindInformationData[numberOfAttributes];
        for (var i = 0; i < numberOfAttributes; i ++)
        {
            int attStart = 2 + i * informationDataLength;
            attributeDataList[i] = new AttFindInformationData(
                BinaryPrimitives.ReadUInt16LittleEndian(source[attStart..]),
                source.Slice(attStart + 2, informationDataLength - 2).ToArray());
        }
        value = new AttFindInformationRsp
        {
            OpCode = opCode,
            Format = format,
            InformationData = attributeDataList,
        };
        bytesRead = source.Length;
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttFindInformationRsp value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out AttFindInformationRsp value, out int bytesRead)
    {
        throw new NotSupportedException();
    }
}