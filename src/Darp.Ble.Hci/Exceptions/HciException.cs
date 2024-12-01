namespace Darp.Ble.Hci.Exceptions;

/// <summary> Represents errors that occur by anything HCI related </summary>
public class HciException : Exception
{
    /// <summary> Initialize a new <see cref="HciException"/> </summary>
    public HciException()
    {
    }

    /// <summary> Initialize a new <see cref="HciException"/> with a message </summary>
    /// <param name="message"> The message of the exception </param>
    public HciException(string message) : base(message)
    {
    }

    /// <summary> Initialize a new <see cref="HciException"/> with a message and exception </summary>
    /// <param name="message"> The message of the exception </param>
    /// <param name="innerException"> The inner exception </param>
    public HciException(string message, Exception innerException) : base(message, innerException)
    {
    }
}