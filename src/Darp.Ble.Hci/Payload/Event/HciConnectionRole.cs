namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The role of the hci connection </summary>
public enum HciConnectionRole : byte
{
    /// <summary> The connection is in central role </summary>
    Central = 0x00,

    /// <summary> The connection is in peripheral role </summary>
    Peripheral = 0x01,
}
