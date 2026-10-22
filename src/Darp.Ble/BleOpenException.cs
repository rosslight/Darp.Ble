using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble;

/// <summary> An adapter could not be opened </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Every open failure has a kind"
)]
public sealed class BleOpenException : BleException
{
    /// <summary> Initializes a new exception </summary>
    /// <param name="kind"> Why the adapter could not be opened </param>
    /// <param name="message"> The message </param>
    /// <param name="isTransient"> Whether opening can succeed later without a change by the user, e.g. a port that is busy for a moment </param>
    /// <param name="nativeCode"> The error code of the platform, if there is one </param>
    /// <param name="innerException"> The cause, if there is one </param>
    public BleOpenException(
        BleOpenErrorKind kind,
        string message,
        bool isTransient = false,
        int? nativeCode = null,
        Exception? innerException = null
    )
        : base(message, isTransient, nativeCode, innerException)
    {
        Kind = kind;
    }

    /// <summary> Why the adapter could not be opened </summary>
    public BleOpenErrorKind Kind { get; }
}
