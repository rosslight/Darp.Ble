using System.Diagnostics;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci.Host;

public sealed class GattServer(HciDevice device, ILogger<GattServer>? logger)
{
    private readonly HciDevice _device = device;
    private readonly ILogger<GattServer>? _logger = logger;

    internal void OnAttExchangeMtuReq(AclConnection connection, AttExchangeMtuReq request)
    {
        using Activity? activity = Logging.StartHandleAttRequestActivity(request, connection);

        ushort newMtu = Math.Min(request.ClientRxMtu, Constants.DefaultMaxAttMtu);
        connection.AttMtu = newMtu;
        connection.EnqueueGattPacket(new AttExchangeMtuRsp { ServerRxMtu = newMtu }, activity, isResponse: true);
    }

    internal void OnHciLeDataLengthChangeEvent(HciLeDataLengthChangeEvent hciEvent)
    {
        _logger?.LogDebug(
            "Connection [0x{ConnectionHandle:X3}]: Received le datachange event: {@PacketData}",
            hciEvent.ConnectionHandle,
            hciEvent
        );
    }
}
