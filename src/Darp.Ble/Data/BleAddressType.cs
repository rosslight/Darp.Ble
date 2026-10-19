namespace Darp.Ble.Data;

/// <summary> The address type as reported by the HCI, e.g. in the LE Extended Advertising Report event </summary>
/// <remarks> The kind of a <see cref="Random"/> address is available via <see cref="BleAddress.RandomKind"/> </remarks>
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
    /// An address that is not registered with the IEEE.
    /// It is either a static, a resolvable private or a non-resolvable private address
    /// </summary>
    Random = 0x01,

    /// <summary>
    /// Public Identity Address:
    /// The controller resolved a resolvable private address and reports the public identity address of the device
    /// </summary>
    PublicIdentity = 0x02,

    /// <summary>
    /// Random (static) Identity Address:
    /// The controller resolved a resolvable private address and reports the static random identity address of the device
    /// </summary>
    RandomStaticIdentity = 0x03,

    /// <summary>
    /// Random Device Address which the controller was unable to resolve.
    /// Only used for the address a directed advertisement is directed to
    /// </summary>
    RandomUnresolved = 0xFE,

    /// <summary> No address provided (anonymous) </summary>
    NotAvailable = 0xFF,
}
