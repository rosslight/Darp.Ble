using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Gatt;

/// <summary> A GATT operation failed: the device answered with an error, does not have what was asked for, or sent a value the declaration cannot decode </summary>
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

    /// <summary> Initializes an exception for a failure without an ATT error, e.g. in tests of an app's error handling </summary>
    /// <param name="kind"> Why the operation failed; not <see cref="GattErrorKind.AttError"/>, which needs its status </param>
    /// <param name="message"> The message </param>
    /// <exception cref="ArgumentException"> The kind is <see cref="GattErrorKind.AttError"/> </exception>
    public GattException(GattErrorKind kind, string message)
        : this(kind, message, innerException: null) { }

    /// <summary> Initializes an exception for a failure without an ATT error, with its cause </summary>
    /// <param name="kind"> Why the operation failed; not <see cref="GattErrorKind.AttError"/>, which needs its status </param>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The cause, if there is one </param>
    internal GattException(GattErrorKind kind, string message, Exception? innerException)
        : base(message, isTransient: false, nativeCode: null, innerException)
    {
        if (kind is GattErrorKind.AttError)
            throw new ArgumentException(
                "An ATT error needs its status; use the constructor that takes it",
                nameof(kind)
            );
        Kind = kind;
    }

    /// <summary> Why the operation failed </summary>
    public GattErrorKind Kind { get; }

    /// <summary> The ATT error the device answered with, when <see cref="Kind"/> is <see cref="GattErrorKind.AttError"/> </summary>
    public GattProtocolStatus? AttStatus { get; }
}
