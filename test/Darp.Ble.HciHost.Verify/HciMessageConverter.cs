using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.HciHost.Verify;

internal sealed class HciMessageConverter : WriteOnlyJsonConverter<HciMessage>
{
    public override void Write(VerifyJsonWriter writer, HciMessage handler)
    {
        writer.WriteStartObject();

        writer.WriteMember(handler, handler.Type, nameof(HciMessage.Type));
        switch (handler.Type)
        {
            case HciPacketType.HciCommand
                when HciCommandPacket.TryReadLittleEndian(handler.PduBytes, out HciCommandPacket packet):
                writer.WriteMember(packet, packet.OpCode, nameof(packet.OpCode));
                break;
            case HciPacketType.HciEvent
                when HciEventPacket.TryReadLittleEndian(handler.PduBytes, out HciEventPacket? eventPacket):
                writer.WriteMember(eventPacket, eventPacket.EventCode, nameof(eventPacket.EventCode));
                switch (eventPacket.EventCode)
                {
                    case HciEventCode.HCI_Command_Complete
                        when HciCommandCompleteEvent.TryReadLittleEndian(
                            eventPacket.DataBytes,
                            out HciCommandCompleteEvent completeEvent
                        ):
                        writer.WriteMember(
                            completeEvent,
                            completeEvent.CommandOpCode,
                            nameof(completeEvent.CommandOpCode)
                        );
                        break;
                    case HciEventCode.HCI_Command_Status
                        when HciCommandStatusEvent.TryReadLittleEndian(
                            eventPacket.DataBytes,
                            out HciCommandStatusEvent statusEvent
                        ):
                        writer.WriteMember(statusEvent, statusEvent.CommandOpCode, nameof(statusEvent.CommandOpCode));
                        break;
                }
                break;
        }
        writer.WriteMember(handler, Convert.ToHexString(handler.PduBytes), nameof(HciMessage.PduBytes));

        writer.WriteEndObject();
    }
}
