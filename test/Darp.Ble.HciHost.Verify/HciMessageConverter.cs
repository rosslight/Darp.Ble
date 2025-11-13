using System.Buffers.Binary;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.HciHost.Verify;

internal sealed class HciMessageConverter : WriteOnlyJsonConverter<HciMessage>
{
    public override void Write(VerifyJsonWriter writer, HciMessage message)
    {
        writer.WriteStartObject();

        writer.WriteMember(message, message.Type, nameof(HciMessage.Type));
        switch (message.Type)
        {
            case HciPacketType.HciCommand
                when HciCommandPacket.TryReadLittleEndian(message.PduBytes, out HciCommandPacket packet):
                writer.WriteMember(packet, packet.OpCode, nameof(packet.OpCode));
                break;
            case HciPacketType.HciEvent
                when HciEventPacket.TryReadLittleEndian(message.PduBytes, out HciEventPacket? eventPacket):
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
            case HciPacketType.HciAclData
                when HciAclPacket.TryReadLittleEndian(message.PduBytes, out HciAclPacket aclPacket)
                    && aclPacket.PacketBoundaryFlag
                        is PacketBoundaryFlag.FirstAutoFlushable
                            or PacketBoundaryFlag.FirstNonAutoFlushable
                    && BinaryPrimitives.TryReadUInt16LittleEndian(aclPacket.DataBytes.Span, out ushort targetLength)
                    && aclPacket.DataBytes.Length == 4 + targetLength
                    && aclPacket.DataBytes.Length > 4:
                writer.WriteMember(aclPacket, (AttOpCode)aclPacket.DataBytes.Span[4], nameof(AttOpCode));
                break;
        }
        writer.WriteMember(
            message,
            Convert.ToHexString(message.PduBytes.Prepend((byte)message.Type).ToArray()),
            nameof(HciMessage.PduBytes)
        );

        writer.WriteEndObject();
    }
}
