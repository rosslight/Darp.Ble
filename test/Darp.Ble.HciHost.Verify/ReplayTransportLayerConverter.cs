namespace Darp.Ble.HciHost.Verify;

internal sealed class ReplayTransportLayerConverter : WriteOnlyJsonConverter<ReplayTransportLayer>
{
    public override void Write(VerifyJsonWriter writer, ReplayTransportLayer value)
    {
        writer.WriteStartObject();

        writer.WriteMember(value, value.MessagesToController, nameof(ReplayTransportLayer.MessagesToController));
        writer.WriteMember(value, value.MessagesToHost, nameof(ReplayTransportLayer.MessagesToHost));

        writer.WriteEndObject();
    }
}
