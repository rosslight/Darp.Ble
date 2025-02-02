using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// TThe HCI_LE_Remove_Advertising_Set command is used to remove an advertising set from the Controller.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeRemoveAdvertisingSetResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-ab6836a2-0c05-804c-bfe5-1121eea4ed38"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeRemoveAdvertisingSetCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Remove_Advertising_Set;
    /// <summary> The Advertising_Handle </summary>
    /// <value> 0x00 to 0xEF </value>
    public required byte AdvertisingHandle { get; init; }
}