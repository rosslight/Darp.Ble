using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Connections;

/// <summary> An operation failed because its connection ended. It carries the same info as the connection's Disconnected task </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Its info is required; a constructor with only a message would leave it unset"
)]
public sealed class BleDisconnectedException : BleException
{
    /// <summary> Initializes an exception for a connection that ended, e.g. in tests of an app's error handling </summary>
    /// <param name="info"> Why the connection ended </param>
    /// <param name="innerException"> The cause, if there is one </param>
    public BleDisconnectedException(DisconnectInfo info, Exception? innerException = null)
        : base(
            MessageOf(info),
            isTransient: info.Reason is not DisconnectReason.LocalRequest,
            info.NativeCode,
            innerException
        )
    {
        Info = info;
    }

    // The message is the first argument of the base call, so the null check runs before the info is read
    private static string MessageOf(DisconnectInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return $"The connection ended: {info.Reason}";
    }

    /// <summary> Why the connection ended </summary>
    public DisconnectInfo Info { get; }
}
