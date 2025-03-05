using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

/// <summary> The HCI Command packet is used to send commands to the Controller from the Host </summary>
/// <typeparam name="TParameters"> The type of the parameters of the command </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-d57613e7-b364-9fe1-0c64-4a992117710f"/>
public sealed class HciCommandPacket<TParameters>(TParameters commandParameters)
    : IHciPacket<HciCommandPacket<TParameters>, TParameters>
    where TParameters : IHciCommand
{
    /// <inheritdoc />
    public static int HeaderLength => 3;

    /// <inheritdoc />
    public static HciPacketType Type => HciPacketType.HciCommand;

    /// <inheritdoc />
    public HciPacketType PacketType => HciPacketType.HciCommand;

    /// <inheritdoc />
    public int GetByteCount() => HeaderLength + Data.GetByteCount();

    /// <inheritdoc />
    public TParameters Data { get; } = commandParameters;

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination)
    {
        return TryWriteLittleEndian(destination, out _);
    }

    /// <inheritdoc />
    public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if (destination.Length < HeaderLength)
            return false;
        var opCode = (ushort)TParameters.OpCode;
        destination[0] = (byte)(opCode & 0xFF);
        destination[1] = (byte)((opCode >> 8) & 0xFF);
        destination[2] = (byte)Data.GetByteCount();
        bytesWritten = 3 + Data.GetByteCount();
        return Data.TryWriteLittleEndian(destination[3..]);
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
    {
        throw new NotSupportedException();
    }
}
