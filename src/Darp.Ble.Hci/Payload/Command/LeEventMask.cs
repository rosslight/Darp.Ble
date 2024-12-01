using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary> Defines the LE_Event_Mask. Can be set via <see cref="HciLeSetEventMaskCommand"/> </summary>
[Flags]
public enum LeEventMask : ulong
{
    /// <summary> No event is specified </summary>
    None = 0,
    /// <summary> Reported when HCI_LE_Create_Connection completes with connection </summary>
    LeConnectionCompleteEvent = 1 << 0,
    /// <summary> Reported when advertising was found after starting scan with HCI_LE_Set_Scan_Enable </summary>
    LeAdvertisingReportEvent = 1 << 1,
    /// <summary> <see cref="HciLeDataLengthChangeEvent"/> </summary>
    LeDataLengthChangeEvent = 1 << 6,
    /// <summary> <see cref="HciLeEnhancedConnectionCompleteV1Event"/> </summary>
    LeEnhancedConnectionCompleteEventV1 = 1 << 9,
}