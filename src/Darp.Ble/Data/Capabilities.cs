namespace Darp.Ble.Data;

/// <summary> There are four GAP roles defined for devices operating over an LE physical transport </summary>
[Flags]
public enum Capabilities
{
    /// <summary> A device with unknown capabilities </summary>
    None = 0,

    /// <summary>
    /// A device operating in the Broadcaster role is a device that sends advertising events or periodic advertising events
    /// </summary>
    Broadcaster = 0b0001,

    /// <summary>
    /// A device operating in the Observer role is a device that receives advertising events or periodic advertising events
    /// </summary>
    Observer = 0b0010,

    /// <summary>
    /// Any device that accepts the establishment of an LE active physical link using any of the connection
    /// establishment procedures is referred to as being in the Peripheral role
    /// </summary>
    Peripheral = 0b0100 | Broadcaster,

    /// <summary>
    /// A device that supports the Central role initiates the establishment of an LE active physical link
    /// </summary>
    Central = 0b1000 | Observer,
}
