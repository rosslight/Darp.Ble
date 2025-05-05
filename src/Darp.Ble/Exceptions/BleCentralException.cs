using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Represents error thrown by a <see cref="Central"/> </summary>
/// <param name="central"> The ble central </param>
/// <param name="message"> The message </param>
public class BleCentralException(BleCentral central, string? message) : Exception(message)
{
    /// <summary> The BleCentral </summary>
    public BleCentral Central { get; } = central;
}
