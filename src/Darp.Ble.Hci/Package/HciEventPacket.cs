using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

public class HciEventPacket(HciEventCode eventCode, byte parameterTotalLength, byte[] dataBytes)
    : IHciEventPacket<HciEventPacket>, IDecodable<HciEventPacket>
{
    public HciEventCode EventCode { get; } = eventCode;
    public byte ParameterTotalLength { get; } = parameterTotalLength;
    public byte[] DataBytes { get; } = dataBytes;

    public override string ToString() => $"{(ushort)EventCode:X2}{ParameterTotalLength:X2}{Convert.ToHexString(DataBytes)}";

    public static bool TryWithData<TParameters>(HciEventPacket hciEventPacket,
        [NotNullWhen(true)] out HciEventPacket<TParameters>? result)
        where TParameters : IHciEvent<TParameters>
    {
        result = null;
        if (hciEventPacket.EventCode != TParameters.EventCode) return false;
        if (!TParameters.TryDecode(hciEventPacket.DataBytes, out TParameters? parameters, out _)) return false;

        result = new HciEventPacket<TParameters>(hciEventPacket.EventCode, hciEventPacket.ParameterTotalLength, hciEventPacket.DataBytes, parameters);
        return true;
    }

    public static bool TryDecode(in ReadOnlyMemory<byte> source, [NotNullWhen(true)] out HciEventPacket? result, out int bytesDecoded)
    {
        const int headerLength = IHciEventPacket<HciEventPacket>.HeaderLength;
        result = null;
        bytesDecoded = default;
        if (source.Length < headerLength) return false;

        ReadOnlySpan<byte> span = source.Span;
        var eventCode = (HciEventCode)span[0];
        byte parameterTotalLength = span[1];
        int totalLength = headerLength + parameterTotalLength;
        if (source.Length < totalLength) return false;
        result = new HciEventPacket(eventCode, parameterTotalLength, source[2..totalLength].ToArray());
        bytesDecoded = totalLength;
        return true;
    }
}

public sealed class HciEventPacket<TData>(HciEventCode eventCode, byte parameterTotalLength, byte[] dataBytes, TData data)
    : HciEventPacket(eventCode, parameterTotalLength, dataBytes)
    where TData : IHciEvent<TData>
{
    public TData Data { get; } = data;
}