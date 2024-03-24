namespace Darp.Ble.Data;

/// <summary> Ble address type </summary>
public enum BleAddressType : byte
{
    /// <summary>
    /// Public Device Address:
    /// A global fixed address that must be registered with the IEEE.
    /// It follows the same guidelines as MAC Addresses and shall be a 48-bit extended unique identifier (EUI-48)
    /// </summary>
    Public = 0x00,
    /// <summary>
    /// Random Device Address:
    /// An address that must NOT be registered with the IEEE.
    /// Can be either fixed for lifetime or be assigned on boot. Cannot change during runtime
    /// </summary>
    RandomStatic = 0x01,
    /// <summary>
    /// Public Identity Address:
    /// An address, which can be resolved by using a key (IRK / Identity Resolving Key).
    /// Purpose is to prevent malicious third-parties from tracking a Bluetooth device while still allowing one or more trusted parties from identifying the Bluetooth device of interest.
    /// A trusted device is a bonded device.
    /// </summary>
    RandomPrivateResolvable = 0x02,
    /// <summary>
    /// Random (static) Identity Address:
    /// An address, which changes periodically.
    /// Unlike a resolvable addresses, it is not resolvable by any other device. The sole purpose of this type of address is to prevent tracking by any other BLE device.
    /// </summary>
    RandomPrivateNonResolvable = 0x03,
    /// <summary> Private resolvable address </summary>
    ResolvablePrivateAddress = 0xFE,
    /// <summary>
    /// No address provided (anonymous)
    /// </summary>
    NotAvailable = 0xFF
}