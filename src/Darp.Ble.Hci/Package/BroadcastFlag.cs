namespace Darp.Ble.Hci.Package;

/// <summary> The Broadcast_Flag </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bc4ffa33-44ef-e93c-16c8-14aa99597cfc"/>
public enum BroadcastFlag
{
    /// <summary> Point-to-point (ACL-U or LE-U) </summary>
    PointToPoint = 0b00,
    /// <summary> BR/EDR broadcast (APB-U) </summary>
    BrEdrBroadcast = 0b01,
}