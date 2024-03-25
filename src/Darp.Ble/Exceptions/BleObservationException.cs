using System.Runtime.InteropServices;

namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents error thrown by a <see cref="BleObserver"/>
/// </summary>
public class BleObservationException : Exception
{
    /// <summary> The BleObserver </summary>
    public BleObserver BleObserver { get; }

    /// <summary> Initializes the new exception </summary>
    /// <param name="bleObserver"> The ble observer </param>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The inner exception </param>
    public BleObservationException(BleObserver bleObserver, string? message, Exception? innerException)
        : base(message, innerException)
    {
        BleObserver = bleObserver;
        if (message is not null)
        {
            Message = message;
        }
        else
        {
            int? hResult = InnerException?.HResult;
            string reason = string.IsNullOrEmpty(InnerException?.Message)
                ? hResult is null ? "unknown" : $"{Marshal.GetExceptionForHR(hResult.Value)?.Message}"
                : InnerException?.Message!;
            Message = $"Error during observation because of: {reason}";
        }
    }

    /// <inheritdoc />
    public override string Message { get; }
}