namespace Darp.Ble;

/// <summary> The base of every exception the library throws for a Bluetooth failure </summary>
/// <remarks> Developer errors are <see cref="NotSupportedException"/> or <see cref="InvalidOperationException"/>, and cancellation is always the caller's <see cref="OperationCanceledException"/> </remarks>
public class BleException : Exception
{
    /// <summary> Initializes a new exception </summary>
    public BleException() { }

    /// <summary> Initializes a new exception with a message </summary>
    /// <param name="message"> The message </param>
    public BleException(string message)
        : base(message) { }

    /// <summary> Initializes a new exception with a message and its cause </summary>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The cause </param>
    public BleException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary> Initializes a new exception </summary>
    /// <param name="message"> The message </param>
    /// <param name="isTransient"> Whether the same operation can succeed later without a change by the user </param>
    /// <param name="nativeCode"> The error code of the platform or controller, if there is one </param>
    /// <param name="innerException"> The cause, if there is one </param>
    public BleException(string message, bool isTransient, int? nativeCode, Exception? innerException)
        : base(message, innerException)
    {
        IsTransient = isTransient;
        NativeCode = nativeCode;
    }

    /// <summary> Whether the same operation can succeed later without a change by the user, e.g. a device out of range </summary>
    public bool IsTransient { get; }

    /// <summary> The error code of the platform or controller, if there is one </summary>
    public int? NativeCode { get; }
}
