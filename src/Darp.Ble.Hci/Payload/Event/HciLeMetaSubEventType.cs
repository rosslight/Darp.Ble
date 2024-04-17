using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Event;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HciLeMetaSubEventType : byte
{
    HCI_LE_Data_Length_Change = 0x07,
    /// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 7.7.65.13 </summary>
    HCI_LE_Extended_Advertising_Report = 0x0D,
    HCI_LE_Enhanced_Connection_Complete_V1 = 0x0A,
    HCI_LE_Enhanced_Connection_Complete_v2 = 0x29,
}