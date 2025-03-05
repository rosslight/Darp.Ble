using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

/// <summary> The HCI Event packet is used by the Controller to notify the Host when events occur </summary>
/// <typeparam name="TSelf"> The type of the implementation </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-5d748f9e-157a-fb06-2469-874a61a5c08c"/>
[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types")]
public interface IHciEventPacket<TSelf> : IHciPacket<TSelf>
    where TSelf : IHciPacket<TSelf>
{
    /// <summary> The header length of a event packet </summary>
    public const int EventPacketHeaderLength = 2;

    static HciPacketType IHciPacket<TSelf>.Type => HciPacketType.HciEvent;
    static int IHciPacket<TSelf>.HeaderLength => EventPacketHeaderLength;

    HciPacketType IHciPacket.PacketType => HciPacketType.HciEvent;

    /// <summary> Each event is assigned a 1-Octet event code used to uniquely identify different types of events. </summary>
    HciEventCode EventCode { get; }

    /// <summary> Length of all the parameters contained in this packet, measured in octets </summary>
    byte ParameterTotalLength { get; }

    /// <summary> All bytes for the event parameters </summary>
    byte[] DataBytes { get; }

    int IBinaryWritable.GetByteCount() => EventPacketHeaderLength + ParameterTotalLength;
    bool IBinaryWritable.TryWriteLittleEndian(Span<byte> destination)
    {
        if (destination.Length < GetByteCount())
            return false;
        destination[0] = (byte)EventCode;
        destination[1] = ParameterTotalLength;
        return DataBytes.AsSpan().TryCopyTo(destination[2..]);
    }
}
