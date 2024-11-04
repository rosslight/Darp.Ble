using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

/// <summary> An abstract hci packet </summary>
public interface IHciPacket : IEncodable
{
    /// <summary> The type of the packet </summary>
    HciPacketType PacketType { get; }
}

/// <summary> An abstract hci packet with a defined self </summary>
/// <typeparam name="TSelf"> The type of the implementation </typeparam>
public interface IHciPacket<TSelf> : IHciPacket
    where TSelf : IHciPacket<TSelf>
{
    /// <summary> The static length of the packet header </summary>
    static abstract int HeaderLength { get; }
    /// <summary> The static type of the packet </summary>
    static abstract HciPacketType Type { get; }
#pragma warning disable CA1033
    HciPacketType IHciPacket.PacketType => TSelf.Type;
#pragma warning restore CA1033
}

/// <summary> An abstract hci packet with a data </summary>
/// <typeparam name="TSelf"> The type of the implementation </typeparam>
/// <typeparam name="TData"> The type of the data of the packet </typeparam>
public interface IHciPacket<TSelf, out TData> : IHciPacket<TSelf>
    where TSelf : IHciPacket<TSelf>
{
    /// <summary> The packet data </summary>
    TData Data { get; }
}