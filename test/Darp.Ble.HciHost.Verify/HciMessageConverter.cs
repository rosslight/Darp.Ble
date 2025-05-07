using Darp.Ble.Hci.Package;

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
        }
        writer.WriteMember(handler, Convert.ToHexString(handler.PduBytes), nameof(HciMessage.PduBytes));

        writer.WriteEndObject();
    }
}
