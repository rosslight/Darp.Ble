using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

public interface IHciPacket : IEncodable
{
    HciPacketType PacketType { get; }
}

public interface IHciPacket<TSelf, out TData> : IHciPacketImpl<TSelf> where TSelf : IHciPacketImpl<TSelf>
{
    TData Data { get; }
}