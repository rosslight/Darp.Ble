namespace Darp.Ble.Hci.Package;

public interface IHciPacketImpl<TSelf> : IHciPacket where TSelf : IHciPacketImpl<TSelf>
{
    static abstract HciPacketType Type { get; }
    HciPacketType IHciPacket.PacketType => TSelf.Type;
    static abstract int HeaderLength { get; }
}