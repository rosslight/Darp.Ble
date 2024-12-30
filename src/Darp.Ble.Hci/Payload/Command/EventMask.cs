namespace Darp.Ble.Hci.Payload.Command;

/// <summary> Defines the LE_Event_Mask. Can be set via <see cref="HciSetEventMaskCommand"/> </summary>
[Flags]
public enum EventMask : ulong
{
    /// <summary> No event is specified </summary>
    None = 0,
}