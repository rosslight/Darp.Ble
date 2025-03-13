using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> LE Meta Sub Event types </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public enum HciLeMetaSubEventType : byte
{
    /// <summary> Invalid sub event type </summary>
    None,

    /// <summary> <see cref="HciLeConnectionUpdateCompleteEvent"/> </summary>
    HCI_LE_Connection_Update_Complete = 0x03,

    /// <summary> <see cref="HciLeDataLengthChangeEvent"/> </summary>
    HCI_LE_Data_Length_Change = 0x07,

    /// <summary> <see cref="HciLeEnhancedConnectionCompleteV1Event"/> </summary>
    HCI_LE_Enhanced_Connection_Complete_V1 = 0x0A,

    /// <summary> The <see cref="HciLePhyUpdateCompleteEvent"/> </summary>
    HCI_LE_PHY_Update_Complete = 0x0C,

    /// <summary> <see cref="HciLeExtendedAdvertisingReportEvent"/> </summary>
    HCI_LE_Extended_Advertising_Report = 0x0D,

    /// <summary> The HCI_LE_Enhanced_Connection_Complete_v2 </summary>
    HCI_LE_Enhanced_Connection_Complete_v2 = 0x29,
}
