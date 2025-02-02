using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Read_Maximum_Advertising_Data_Length command is used to read the maximum length of data supported by the Controller for use as advertisement data or scan response data in an advertising event or as periodic advertisement data.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeReadMaximumAdvertisingDataLengthResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e1de0ec4-eba6-5365-4f6a-0de9d5bfb7be"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeReadMaximumAdvertisingDataLengthCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_READ_MAXIMUM_ADVERTISING_DATA_LENGTH;
}