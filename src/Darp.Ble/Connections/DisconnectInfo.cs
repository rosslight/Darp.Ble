namespace Darp.Ble.Connections;

/// <summary> Why and how a connection ended </summary>
/// <param name="Reason"> Why the connection ended </param>
/// <param name="NativeCode"> The error code of the platform or controller, if there is one </param>
public sealed record DisconnectInfo(DisconnectReason Reason, int? NativeCode = null);
