using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble;

/// <summary> The GAP roles and the optional features of an adapter </summary>
[Flags]
[SuppressMessage(
    "Usage",
    "CA2217:Do not mark enums with FlagsAttribute",
    Justification = "Peripheral includes Broadcaster and Central includes Observer, as the roles require"
)]
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

    /// <summary> The adapter can set its own random address and that of its advertising sets </summary>
    SetRandomAddress = 0b1_0000,
}
