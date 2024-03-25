namespace Darp.Ble.Exceptions;

/// <summary> Thrown if start of advertising observation was unsuccessful </summary>
public sealed class BleObservationStartException : BleObservationException
{
    /// <summary> Initializes the new exception </summary>
    /// <param name="bleObserver"> The ble observer </param>
    /// <param name="message"> The message </param>
    public BleObservationStartException(BleObserver bleObserver, string message)
        : base(bleObserver, message, innerException: null)
    {
    }
}