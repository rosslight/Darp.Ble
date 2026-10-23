using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Connections;

/// <summary> A peripheral could not be connected </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Every connect failure has a kind"
)]
public sealed class BleConnectException : BleException
{
    /// <summary> Initializes a new exception. Unreachable, TimedOut and Busy are transient: trying again later can succeed </summary>
    /// <param name="kind"> Why the peripheral could not be connected </param>
    /// <param name="message"> The message </param>
    /// <param name="nativeCode"> The error code of the platform or controller, if there is one </param>
    /// <param name="innerException"> The cause, if there is one </param>
    public BleConnectException(
        BleConnectErrorKind kind,
        string message,
        int? nativeCode = null,
        Exception? innerException = null
    )
        : base(message, isTransient: kind is not BleConnectErrorKind.Rejected, nativeCode, innerException)
    {
        Kind = kind;
    }

    /// <summary> Why the peripheral could not be connected </summary>
    public BleConnectErrorKind Kind { get; }
}
