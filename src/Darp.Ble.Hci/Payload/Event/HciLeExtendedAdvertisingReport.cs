using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> All parameters of the <see cref="HciLeExtendedAdvertisingReportEvent"/> </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e88970dc-edc8-ca27-58d8-153b97751686"/>
[BinaryObject]
public readonly partial record struct HciLeExtendedAdvertisingReport
{
    /// <summary> The Event_Type </summary>
    public required ushort EventType { get; init; }
    /// <summary> The Address_Type </summary>
    public required byte AddressType { get; init; }
    /// <summary> The Address </summary>
    public required DeviceAddress Address { get; init; }
    /// <summary> The Primary_PHY </summary>
    public required byte PrimaryPhy { get; init; }
    /// <summary> The Secondary_PHY </summary>
    public required byte SecondaryPhy { get; init; }
    /// <summary> The Advertising_SID </summary>
    public required byte AdvertisingSId { get; init; }
    /// <summary> The TX_Power </summary>
    public required sbyte TxPower { get; init; }
    /// <summary> The RSSI </summary>
    public required sbyte Rssi { get; init; }
    /// <summary> The Periodic_Advertising_Interval </summary>
    public required ushort PeriodicAdvertisingInterval { get; init; }
    /// <summary> The Direct_Address_Type </summary>
    public required byte DirectAddressType { get; init; }
    /// <summary> The Direct_Address </summary>
    public required DeviceAddress DirectAddress { get; init; }
    /// <summary> The Data_Length </summary>
    public required byte DataLength { get; init; }
    /// <summary> The Data </summary>
    [BinaryElementCount(nameof(DataLength))]
    public required ReadOnlyMemory<byte> Data { get; init; }
}