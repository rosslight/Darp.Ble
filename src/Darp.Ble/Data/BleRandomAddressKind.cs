namespace Darp.Ble.Data;

/// <summary> The kind of random device address, given by the two most significant bits of the address </summary>
public enum BleRandomAddressKind
{
    /// <summary> Static Device Address (0b11): Fixed for the lifetime or changed only on power-up </summary>
    Static,

    /// <summary> Resolvable Private Address (0b01): Changes periodically and can be resolved with the identity resolving key (IRK) of the device </summary>
    ResolvablePrivate,

    /// <summary> Non-Resolvable Private Address (0b00): Changes periodically and cannot be resolved by any device </summary>
    NonResolvablePrivate,
}
