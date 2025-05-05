using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents error thrown by a <see cref="BleBroadcaster"/>
/// </summary>
/// <param name="broadcaster"> The ble broadcaster </param>
/// <param name="message"> The message </param>
public class BleBroadcasterException(IBleBroadcaster broadcaster, string? message) : Exception(message)
{
    /// <summary> The BleCentral </summary>
    public IBleBroadcaster Broadcaster { get; } = broadcaster;
}
