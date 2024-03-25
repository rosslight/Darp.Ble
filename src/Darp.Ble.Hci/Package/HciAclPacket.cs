using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 5.4.2 HCI ACL Data packets </summary>
public enum PacketBoundaryFlag
{
    /// <summary>
    /// First non-automatically-flushable packet of a higher layer message
    /// (start of a non-automatically-flushable L2CAP PDU) from Host to Controller.
    /// </summary>
    FirstNonAutoFlushable = 0b00,
    /// <summary>
    /// Continuing fragment of a higher layer message
    /// </summary>
    ContinuingFragment = 0b01,
    /// <summary>
    /// First automatically flushable packet of a higher layer message (start of an automatically-flushable L2CAP PDU)
    /// </summary>
    FirstAutoFlushable = 0b10,
}

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 5.4.2 HCI ACL Data packets </summary>
public enum BroadcastFlag
{
    /// <summary> Point-to-point (ACL-U or LE-U) </summary>
    PointToPoint = 0b00,
    /// <summary> BR/EDR broadcast (APB-U) </summary>
    BrEdrBroadcast = 0b01,
}

public class HciAclPacket(
    ushort connectionHandle,
    PacketBoundaryFlag packetBoundaryFlag,
    BroadcastFlag broadcastFlag,
    ushort dataTotalLength,
    byte[] dataBytes) : IHciPacketImpl<HciAclPacket>, IDecodable<HciAclPacket>
{
    public static HciPacketType Type => HciPacketType.HciAclData;
    public static int HeaderLength => 4;
    public int Length => HeaderLength + DataTotalLength;

    public ushort ConnectionHandle { get; } = connectionHandle;
    public PacketBoundaryFlag PacketBoundaryFlag { get; } = packetBoundaryFlag;
    public BroadcastFlag BroadcastFlag { get; } = broadcastFlag;
    public ushort DataTotalLength { get; } = dataTotalLength;
    public byte[] DataBytes { get; } = dataBytes;

    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        var firstBytes = (ushort)(ConnectionHandle | (byte)PacketBoundaryFlag << 12 | (byte)BroadcastFlag << 14);
        BinaryPrimitives.WriteUInt16LittleEndian(destination, firstBytes);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], DataTotalLength);
        return DataBytes.AsSpan().TryCopyTo(destination[4..]);
    }

    public static bool TryDecode(in ReadOnlyMemory<byte> source, [NotNullWhen(true)] out HciAclPacket? result, out int bytesDecoded)
    {
        result = null;
        bytesDecoded = default;
        if (source.Length < 4) return false;
        ReadOnlySpan<byte> span = source.Span;
        ushort firstBytes = BinaryPrimitives.ReadUInt16LittleEndian(span);
        var connectionHandle = (ushort)(firstBytes & 0xFFF);
        var packetBoundaryFlag = (PacketBoundaryFlag)((firstBytes & 0b11000000000000) >> 12);
        var broadcastFlag = (BroadcastFlag)((firstBytes & 0b1100000000000000) >> 14);
        ushort totalLength = BinaryPrimitives.ReadUInt16LittleEndian(span[2..]);
        if (source.Length != 4 + totalLength) return false;
        result = new HciAclPacket(connectionHandle,
            packetBoundaryFlag,
            broadcastFlag,
            totalLength,
            span[4..(4 + totalLength)].ToArray());
        return true;
    }

    public override string ToString() => Convert.ToHexString((this as IEncodable).ToByteArray());
}

public sealed class HciAclPacket<TData>(ushort connectionHandle,
    PacketBoundaryFlag packetBoundaryFlag,
    BroadcastFlag broadcastFlag,
    ushort dataTotalLength,
    TData data)
    : HciAclPacket(connectionHandle, packetBoundaryFlag, broadcastFlag, dataTotalLength, data.ToByteArray()),
        IHciPacket<HciAclPacket<TData>, TData>
    where TData : IEncodable
{
    public HciPacketType PacketType => Type;
    static HciPacketType IHciPacketImpl<HciAclPacket<TData>>.Type => Type;
    static int IHciPacketImpl<HciAclPacket<TData>>.HeaderLength => HeaderLength;

    public TData Data { get; } = data;
}