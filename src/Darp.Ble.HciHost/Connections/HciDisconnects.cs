using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.HciHost.Connections;

/// <summary> Taking links down and the reasons they went down </summary>
internal static class HciDisconnects
{
    /// <summary> Why the controller reports a link down </summary>
    public static DisconnectInfo ToInfo(HciCommandStatus reason) =>
        new(
            (byte)reason switch
            {
                (byte)HciCommandStatus.ConnectionTerminatedByLocalHost => DisconnectReason.LocalRequest,
                // Supervision timeout, LL response timeout, failed to be established
                0x08 or 0x22 or 0x3E => DisconnectReason.ConnectionTimeout,
                _ => DisconnectReason.RemoteRequest,
            },
            (int)reason
        );

    /// <summary> Asks the controller to take a link down; its Disconnection Complete follows </summary>
    public static async Task RequestAsync(HciDevice device, ushort connectionHandle)
    {
        HciCommandStatus status = await device
            .Host.QueryCommandStatusAsync(
                new HciDisconnectCommand
                {
                    ConnectionHandle = connectionHandle,
                    Reason = HciCommandStatus.RemoteUserTerminatedConnection,
                },
                CancellationToken.None
            )
            .ConfigureAwait(false);
        // Unknown Connection Identifier: the link went down meanwhile, and its event is on the way
        if (status is not (HciCommandStatus.Success or HciCommandStatus.UnknownConnectionIdentifier))
            throw HciCommands.Failed(status, "disconnect");
    }
}
