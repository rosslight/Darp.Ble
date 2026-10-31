namespace Darp.Ble.Connections;

/// <summary> Why a connection ended </summary>
public enum DisconnectReason
{
    /// <summary> This app ended it: <c>DisconnectAsync</c>, disposing the connection or its adapter </summary>
    LocalRequest,

    /// <summary> The other device ended it </summary>
    RemoteRequest,

    /// <summary> The link was lost because the other device stopped answering (supervision timeout) </summary>
    ConnectionTimeout,

    /// <summary> The other device did not answer a GATT request within 30 s </summary>
    AttTimeout,

    /// <summary> The transport to the adapter failed, e.g. the dongle was unplugged </summary>
    TransportLost,
}
