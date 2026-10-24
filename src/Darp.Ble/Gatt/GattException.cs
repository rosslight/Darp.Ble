using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A GATT operation failed: the device answered with an error, or it does not have what was asked for </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Every GATT failure has a kind"
)]
public sealed class GattException : BleException
{
    /// <summary> Initializes an exception for an ATT error the device answered with </summary>
    /// <param name="attStatus"> The ATT error </param>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The cause, if there is one </param>
    public GattException(GattProtocolStatus attStatus, string message, Exception? innerException = null)
        : base(message, isTransient: false, nativeCode: null, innerException)
    {
        Kind = GattErrorKind.AttError;
        AttStatus = attStatus;
    }

    internal GattException(GattErrorKind kind, string message)
        : base(message, isTransient: false, nativeCode: null, innerException: null)
    {
        Kind = kind;
    }

    /// <summary> Why the operation failed </summary>
    public GattErrorKind Kind { get; }

    /// <summary> The ATT error the device answered with, when <see cref="Kind"/> is <see cref="GattErrorKind.AttError"/> </summary>
    public GattProtocolStatus? AttStatus { get; }
}
