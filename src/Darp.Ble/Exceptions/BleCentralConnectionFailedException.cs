using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Represents error thrown when connection attempt has failed </summary>
public class BleCentralConnectionFailedException : BleCentralException
{
    /// <summary> Initialize a new exception when a connection attempt has failed </summary>
    /// <param name="central"> The central responsible for connection </param>
    /// <param name="message"> The message </param>
    public BleCentralConnectionFailedException(BleCentral central, string? message)
        : base(central, message) { }

    /// <summary> Initialize a new exception when a connection attempt has failed </summary>
    /// <param name="central"> The central responsible for connection </param>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The inner exception </param>
    public BleCentralConnectionFailedException(BleCentral central, string? message, Exception innerException)
        : base(central, message, innerException) { }
}
