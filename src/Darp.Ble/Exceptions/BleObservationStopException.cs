namespace Darp.Ble.Exceptions;

/// <summary> Thrown if something went wrong during stopping the observation </summary>
public sealed class BleObservationStopException : BleObservationException
{
    /// <summary> Initializes the new exception </summary>
    /// <param name="bleObserver"> The ble observer </param>
    /// <param name="message"> The message </param>
    public BleObservationStopException(BleObserver bleObserver, string message)
        : base(bleObserver, message, innerException: null)
    {
    }
}