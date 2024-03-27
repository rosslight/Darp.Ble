namespace Darp.Ble.Hci.Package;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 5.4.2 HCI ACL Data packets </summary>
public enum BroadcastFlag
{
    /// <summary> Point-to-point (ACL-U or LE-U) </summary>
    PointToPoint = 0b00,
    /// <summary> BR/EDR broadcast (APB-U) </summary>
    BrEdrBroadcast = 0b01,
}