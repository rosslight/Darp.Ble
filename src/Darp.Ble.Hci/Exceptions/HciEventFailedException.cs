using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Exceptions;

public sealed class HciEventFailedException(HciEventPacket<HciCommandStatusEvent> packet)
    : HciException($"Got failure response for command {packet.Data.CommandOpCode} with status {packet.Data.Status}")
{
    public HciEventPacket<HciCommandStatusEvent> Packet { get; } = packet;
}