using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Represents error thrown when connection attempt has failed </summary>
public sealed class BleCentralConnectionFailedException : BleCentralException
{
    /// <summary> Initialize a new exception when a connection attempt has failed </summary>
    /// <param name="central"> The central responsible for connection </param>
    /// <param name="message"> The message </param>
    public BleCentralConnectionFailedException(BleCentral central, string? message) : base(central, message)
    {
    }
}