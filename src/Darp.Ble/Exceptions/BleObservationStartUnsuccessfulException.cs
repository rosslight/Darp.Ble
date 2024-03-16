using System.Runtime.InteropServices;

namespace Darp.Ble.Exceptions;

/// <summary> Thrown if start of advertising observation was unsuccessful </summary>
public sealed class BleObservationStartUnsuccessfulException : Exception
{
    /// <summary> The BleObserver </summary>
    public BleObserver BleObserver { get; }

    /// <summary> Initializes the new exception </summary>
    /// <param name="bleObserver"> The ble observer </param>
    /// <param name="innerException"> The inner exception </param>
    public BleObservationStartUnsuccessfulException(BleObserver bleObserver, Exception innerException)
        : base(null, innerException)
    {
        BleObserver = bleObserver;
    }

    /// <inheritdoc />
    public override string Message
    {
        get
        {
            int? hResult = InnerException?.HResult;
            string reason = string.IsNullOrEmpty(InnerException?.Message)
                ? hResult is null
                    ? "unknown"
                    : $"{Marshal.GetExceptionForHR(hResult.Value)?.Message}"
                : InnerException?.Message!;
            return $"Could not start observation because of: {reason}";
        }
    }
}