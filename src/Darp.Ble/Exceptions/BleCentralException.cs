using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Represents error thrown by a <see cref="Central"/> </summary>
public abstract class BleCentralException : Exception
{
    /// <summary> Initialize a new <see cref="BleCentralException"/> </summary>
    /// <param name="central"> The ble central </param>
    /// <param name="message"> The message </param>
    protected BleCentralException(BleCentral central, string? message)
        : base(message)
    {
        Central = central;
    }

    /// <summary> Initialize a new <see cref="BleCentralException"/> </summary>
    /// <param name="central"> The ble central </param>
    /// <param name="message"> The message </param>
    /// <param name="innerException"> The inner exception </param>
    protected BleCentralException(BleCentral central, string? message, Exception innerException)
        : base(message, innerException)
    {
        Central = central;
    }

    /// <summary> The BleCentral </summary>
    public BleCentral Central { get; }
}
