using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

/// <summary> HCI ACL Data packets are used to exchange data between the Host and Controller </summary>
/// <param name="connectionHandle"> Connection_Handle to be used for transmitting a data packet over a Controller. </param>
/// <param name="packetBoundaryFlag"> The Packet_Boundary_Flag </param>
/// <param name="broadcastFlag"> The Broadcast_Flag </param>
/// <param name="dataTotalLength"> The Data_Total_Length </param>
/// <param name="dataBytes"> The actual data </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bc4ffa33-44ef-e93c-16c8-14aa99597cfc"/>
[BinaryObject]
public partial class HciAclPacket(
    ushort connectionHandle,
    PacketBoundaryFlag packetBoundaryFlag,
    BroadcastFlag broadcastFlag,
    ushort dataTotalLength,
    byte[] dataBytes) : IHciPacket<HciAclPacket>
{
    /// <inheritdoc />
    public static HciPacketType Type => HciPacketType.HciAclData;

    /// <inheritdoc />
    public static int HeaderLength => 4;

    /// <inheritdoc />
    public int Length => HeaderLength + DataTotalLength;

    /// <summary> Connection_Handle to be used for transmitting a data packet over a Controller </summary>
    /// <value> Range: 0x0000 to 0x0EFF </value>
    public ushort ConnectionHandle { get; } = connectionHandle;
    /// <summary> The Packet_Boundary_Flag </summary>
    public PacketBoundaryFlag PacketBoundaryFlag { get; } = packetBoundaryFlag;
    /// <summary> The Broadcast_Flag </summary>
    public BroadcastFlag BroadcastFlag { get; } = broadcastFlag;
    /// <summary> The Data_Total_Length </summary>
    public ushort DataTotalLength { get; } = dataTotalLength;
    /// <summary> The actual data </summary>
    public byte[] DataBytes { get; } = dataBytes;

    /// <inheritdoc />
    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        var firstBytes = (ushort)(ConnectionHandle | (byte)PacketBoundaryFlag << 12 | (byte)BroadcastFlag << 14);
        BinaryPrimitives.WriteUInt16LittleEndian(destination, firstBytes);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], DataTotalLength);
        return DataBytes.AsSpan().TryCopyTo(destination[4..]);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override string ToString() => Convert.ToHexString((this as IEncodable).ToByteArray());
}

/// <summary> HCI ACL Data packets are used to exchange data between the Host and Controller </summary>
/// <param name="connectionHandle"> Connection_Handle to be used for transmitting a data packet over a Controller. </param>
/// <param name="packetBoundaryFlag"> The Packet_Boundary_Flag </param>
/// <param name="broadcastFlag"> The Broadcast_Flag </param>
/// <param name="dataTotalLength"> The Data_Total_Length </param>
/// <param name="data"> The actual data </param>
/// <typeparam name="TData"> The type of the data </typeparam>
public sealed class HciAclPacket<TData>(ushort connectionHandle,
    PacketBoundaryFlag packetBoundaryFlag,
    BroadcastFlag broadcastFlag,
    ushort dataTotalLength,
    TData data)
    : HciAclPacket(connectionHandle, packetBoundaryFlag, broadcastFlag, dataTotalLength, data.ToByteArray()),
        IHciPacket<HciAclPacket<TData>, TData>
    where TData : IEncodable
{
    /// <inheritdoc />
    public HciPacketType PacketType => Type;
    static HciPacketType IHciPacket<HciAclPacket<TData>>.Type => Type;
    static int IHciPacket<HciAclPacket<TData>>.HeaderLength => HeaderLength;

    /// <inheritdoc />
    public TData Data { get; } = data;
}
