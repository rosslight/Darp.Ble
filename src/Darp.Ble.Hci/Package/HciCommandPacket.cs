using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

/// <summary>
/// Try to send a command. See BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E, 5.4.1
/// </summary>
/// <typeparam name="TParameters">The type of the parameters of the command</typeparam>
public sealed class HciCommandPacket<TParameters> : IHciPacket<HciCommandPacket<TParameters>, TParameters>
    where TParameters : unmanaged, IHciCommand<TParameters>
{
    public static int HeaderLength => 3;
    public static HciPacketType Type => HciPacketType.HciCommand;
    public HciPacketType PacketType => HciPacketType.HciCommand;

    public HciCommandPacket(TParameters commandParameters)
    {
        Data = commandParameters;
    }

    public int Length => HeaderLength + Data.Length;

    public bool TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        var opCode = (ushort)TParameters.OpCode;
        destination[0] = (byte)(opCode & 0xFF);
        destination[1] = (byte)((opCode >> 8) & 0xFF);
        destination[2] = (byte)Data.Length;
        return Data.TryEncode(destination[3..]);
    }

    public TParameters Data { get; }
}