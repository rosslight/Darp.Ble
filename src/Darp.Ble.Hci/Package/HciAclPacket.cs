using System.Buffers.Binary;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Package;

/// <summary> HCI ACL Data packets are used to exchange data between the Host and Controller </summary>
/// <param name="connectionHandle"> Connection_Handle to be used for transmitting a data packet over a Controller. </param>
/// <param name="packetBoundaryFlag"> The Packet_Boundary_Flag </param>
/// <param name="broadcastFlag"> The Broadcast_Flag </param>
/// <param name="dataTotalLength"> The Data_Total_Length </param>
/// <param name="dataBytes"> The actual data </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bc4ffa33-44ef-e93c-16c8-14aa99597cfc"/>
public readonly struct HciAclPacket(
    ushort connectionHandle,
    PacketBoundaryFlag packetBoundaryFlag,
    BroadcastFlag broadcastFlag,
    ushort dataTotalLength,
    ReadOnlyMemory<byte> dataBytes
) : IHciPacket<HciAclPacket>, IBinaryReadable<HciAclPacket>
{
    /// <inheritdoc />
    public static HciPacketType Type => HciPacketType.HciAclData;

    /// <inheritdoc />
    public static int HeaderLength => 4;

    /// <inheritdoc />
    public int GetByteCount() => HeaderLength + DataTotalLength;

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
    public ReadOnlyMemory<byte> DataBytes { get; } = dataBytes;

    /// <inheritdoc />
    public override string ToString()
    {
        return Convert.ToHexString(this.ToArrayLittleEndian());
    }

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination) => TryWriteLittleEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if (destination.Length < GetByteCount())
            return false;
        var firstBytes = (ushort)(ConnectionHandle | (byte)PacketBoundaryFlag << 12 | (byte)BroadcastFlag << 14);
        BinaryPrimitives.WriteUInt16LittleEndian(destination, firstBytes);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], DataTotalLength);
        bytesWritten += 4;
        if (!DataBytes.Span.TryCopyTo(destination[4..]))
            return false;
        bytesWritten += DataBytes.Length;
        return true;
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination) => TryWriteBigEndian(destination, out _);

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotSupportedException();

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciAclPacket value) =>
        TryReadLittleEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciAclPacket value, out int bytesRead)
    {
        value = default;
        bytesRead = 0;
        if (source.Length < 4)
            return false;
        ushort firstBytes = BinaryPrimitives.ReadUInt16LittleEndian(source);
        var connectionHandle = (ushort)(firstBytes & 0xFFF);
        var packetBoundaryFlag = (PacketBoundaryFlag)((firstBytes & 0b11000000000000) >> 12);
        var broadcastFlag = (BroadcastFlag)((firstBytes & 0b1100000000000000) >> 14);
        ushort totalLength = BinaryPrimitives.ReadUInt16LittleEndian(source[2..]);
        if (source.Length != 4 + totalLength)
            return false;
        value = new HciAclPacket(
            connectionHandle,
            packetBoundaryFlag,
            broadcastFlag,
            totalLength,
            source[4..(4 + totalLength)].ToArray()
        );
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciAclPacket value) =>
        TryReadBigEndian(source, out value, out _);

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciAclPacket value, out int bytesRead) =>
        throw new NotSupportedException();
}
