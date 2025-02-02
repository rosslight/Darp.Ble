using Darp.Ble.Gap;

namespace Darp.Ble.Exceptions;

/// <summary>
/// Represents error thrown by a <see cref="Advertisement"/>
/// </summary>
public class BleAdvertisementException : Exception
{
    /// <summary> The Advertisement </summary>
    public IGapAdvertisement Advertisement { get; }

    /// <summary> Initializes the new exception </summary>
    /// <param name="advertisement"> The ble advertisement </param>
    /// <param name="message"> The message </param>
    public BleAdvertisementException(IGapAdvertisement advertisement, string? message)
        : base(message)
    {
        Advertisement = advertisement;
    }
}
