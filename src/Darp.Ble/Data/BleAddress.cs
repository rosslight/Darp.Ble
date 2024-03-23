using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

/// <summary> The ble address </summary>
public readonly struct BleAddress
{
    /// <summary> Initializes a new ble address </summary>
    /// <param name="address"> The 48bit address </param>
    [SetsRequiredMembers]
    public BleAddress(UInt48 address) : this(BleAddressType.NotAvailable, address) {}

    /// <summary> Initializes a new ble address with a given type </summary>
    /// <param name="type"> The type of the address </param>
    /// <param name="address"> The 48bit address </param>
    [SetsRequiredMembers]
    public BleAddress(BleAddressType type, UInt48 address)
    {
        Type = type;
        Value = address;
    }

    /// <summary> The type of the address </summary>
    public required BleAddressType Type { get; init; } = BleAddressType.NotAvailable;
    /// <summary> The 48bit address </summary>
    public required UInt48 Value { get; init; }

    /// <summary> Convert the ble address to it's underlying value </summary>
    /// <param name="bleAddress"> The ble address </param>
    /// <returns> The 48 bit address </returns>
    public static implicit operator UInt48(BleAddress bleAddress) => bleAddress.Value;
    /// <summary> Convert the 48bit value to a ble address </summary>
    /// <param name="value"> The 48 bit address </param>
    /// <returns> A ble address </returns>
    public static explicit operator BleAddress(UInt48 value) => new(value);
}