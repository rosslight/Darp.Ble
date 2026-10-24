using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Connections;

/// <summary> An operation failed because its connection ended. It carries the same info as the connection's Disconnected task </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Thrown by the library only"
)]
public sealed class BleDisconnectedException : BleException
{
    internal BleDisconnectedException(DisconnectInfo info, Exception? innerException = null)
        : base(
            $"The connection ended: {info.Reason}",
            isTransient: info.Reason is not DisconnectReason.LocalRequest,
            info.NativeCode,
            innerException
        )
    {
        Info = info;
    }

    /// <summary> Why the connection ended </summary>
    public DisconnectInfo Info { get; }
}
