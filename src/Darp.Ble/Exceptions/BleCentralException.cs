namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents error thrown by a <see cref="Central"/>
/// </summary>
public class BleCentralException : Exception
{
    /// <summary> The BleCentral </summary>
    public BleCentral Central { get; }

    /// <summary> Initializes the new exception </summary>
    /// <param name="central"> The ble central </param>
    /// <param name="message"> The message </param>
    public BleCentralException(BleCentral central, string? message) : base(message)
    {
        Central = central;
    }
}