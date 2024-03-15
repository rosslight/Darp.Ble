using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Data;

public readonly struct BleAddress
{

    [SetsRequiredMembers]
    public BleAddress(UInt48 address) : this(BleAddressType.NotAvailable, address) {}

    [SetsRequiredMembers]
    public BleAddress(BleAddressType type, UInt48 address)
    {
        Type = type;
        Value = address;
    }

    public required BleAddressType Type { get; init; }
    public required UInt48 Value { get; init; }

    public static implicit operator UInt48(BleAddress pduType) => pduType.Value;
    public static explicit operator BleAddress(UInt48 value) => new(value);
}