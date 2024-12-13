using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

/// <summary> The HCI Event packet is used by the Controller to notify the Host when events occur </summary>
/// <param name="eventCode"> The code of the event </param>
/// <param name="parameterTotalLength"> The Parameter_Total_Length </param>
/// <param name="dataBytes"> All bytes for the event parameters </param>
[BinaryObject]
public partial class HciEventPacket(HciEventCode eventCode, byte parameterTotalLength, byte[] dataBytes)
    : IHciEventPacket<HciEventPacket>
{
    /// <inheritdoc />
    public HciEventCode EventCode { get; } = eventCode;

    /// <inheritdoc />
    public byte ParameterTotalLength { get; } = parameterTotalLength;

    /// <inheritdoc />
    public byte[] DataBytes { get; } = dataBytes;

    /// <inheritdoc />
    public override string ToString() => $"{(ushort)EventCode:X2}{ParameterTotalLength:X2}{Convert.ToHexString(DataBytes)}";

    /// <summary> Create a new event packet for the given parameter type </summary>
    /// <param name="hciEventPacket"> The event packet </param>
    /// <param name="result"> The packet with decoded parameters </param>
    /// <typeparam name="TParameters"> The type of the parameters </typeparam>
    /// <returns> True, when decoding was successful </returns>
    public static bool TryWithData<TParameters>(HciEventPacket hciEventPacket,
        [NotNullWhen(true)] out HciEventPacket<TParameters>? result)
        where TParameters : IHciEvent<TParameters>
    {
        ArgumentNullException.ThrowIfNull(hciEventPacket);
        result = null;
        if (hciEventPacket.EventCode != TParameters.EventCode) return false;
        if (!TParameters.TryDecode(hciEventPacket.DataBytes, out TParameters? parameters, out _)) return false;

        result = new HciEventPacket<TParameters>(hciEventPacket.EventCode,
            hciEventPacket.ParameterTotalLength,
            hciEventPacket.DataBytes,
            parameters);
        return true;
    }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source, [NotNullWhen(true)] out HciEventPacket? result, out int bytesDecoded)
    {
        const int headerLength = IHciEventPacket<HciEventPacket>.EventPacketHeaderLength;
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

/// <summary> The event packet with decoded parameters </summary>
/// <param name="eventCode"> The code of the event </param>
/// <param name="parameterTotalLength"> The Parameter_Total_Length </param>
/// <param name="dataBytes"> All bytes for the event parameters </param>
/// <param name="data"> The event parameters </param>
/// <typeparam name="TParameters"> The type of the parameters </typeparam>
public sealed class HciEventPacket<TParameters>(HciEventCode eventCode, byte parameterTotalLength, byte[] dataBytes, TParameters data)
    : HciEventPacket(eventCode, parameterTotalLength, dataBytes)
    where TParameters : IHciEvent<TParameters>
{
    /// <summary> The event parameters </summary>
    public TParameters Data { get; } = data;
}