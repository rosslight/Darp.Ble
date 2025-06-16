using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload;

/// <summary> An HCI Event code </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-015badeb-0ba5-17ac-3d39-81a1b56047c1"/>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public enum HciEventCode : byte
{
    /// <summary> No event code was given </summary>
    None = 0x00,

    /// <summary> The <see cref="HciDisconnectionCompleteEvent"/> </summary>
    HCI_Disconnection_Complete = 0x05,

    /// <summary> The <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    HCI_Command_Complete = 0x0E,

    /// <summary> The <see cref="HciCommandStatusEvent"/> </summary>
    HCI_Command_Status = 0x0F,

    /// <summary> The <see cref="HciNumberOfCompletedPacketsEvent"/> </summary>
    HCI_Number_Of_Completed_Packets = 0x13,

    /// <summary> The <see cref="HciLeMetaEvent"/> </summary>
    HCI_LE_Meta = 0x3E,
}
