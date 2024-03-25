using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HciEventCode : byte
{
    HCI_Disconnection_Complete = 0x05,
    /// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E | 7.7.14 </summary>
    HCI_Command_Complete = 0x0E,
    HCI_Command_Status = 0x0F,
    HCI_Number_Of_Completed_Packets = 0x13,
    HCI_LE_Meta = 0x3E,
}