using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

public interface IHciEventPacket<TSelf> : IHciPacketImpl<TSelf> where TSelf : IHciPacketImpl<TSelf>
{
    new const int HeaderLength = 2;
    static HciPacketType IHciPacketImpl<TSelf>.Type => HciPacketType.HciEvent;
    static int IHciPacketImpl<TSelf>.HeaderLength => HeaderLength;

    HciPacketType IHciPacket.PacketType => HciPacketType.HciEvent;
    HciEventCode EventCode { get; }
    byte ParameterTotalLength { get; }
    byte[] DataBytes { get; }

    int IEncodable.Length => HeaderLength + ParameterTotalLength;
    bool IEncodable.TryEncode(Span<byte> destination)
    {
        if (destination.Length < Length) return false;
        destination[0] = (byte)EventCode;
        destination[1] = ParameterTotalLength;
        return DataBytes.AsSpan().TryCopyTo(destination[2..]);
    }
}

public interface IHciEventPacket<TSelf, out TData> : IHciEventPacket<TSelf>, IHciPacket<TSelf, TData>
    where TSelf : IHciPacketImpl<TSelf>;