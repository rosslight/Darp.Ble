using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Advertising_Set_Random_Address command is used by the Host to set the random device address specified by the Random_Address parameter
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetAdvertisingSetRandomAddressResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-933c7de3-2f57-68f0-17d5-7ec752449563"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetAdvertisingSetRandomAddressCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_SET_ADVERTISING_SET_RANDOM_ADDRESS;
    /// <summary> Advertising_Handle Used to identify an advertising set </summary>
    /// <value> 0x00 to 0xEF </value>
    public required byte AdvertisingHandle { get; init; }
    /// <summary> Random_Address </summary>
    public required UInt48 RandomAddress { get; init; }
}