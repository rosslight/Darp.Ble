using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Read_Number_of_Supported_Advertising_Sets command is used to read the maximum number of advertising sets supported by the advertising Controller at the same time.
/// Note: The number of advertising sets that can be supported is not fixed and the Controller can change it at any time because the memory used to store advertising sets can also be used for other purposes.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciSetEventMaskResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-9a2f6557-c845-e470-9cd4-6b968dc8132a"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeReadNumberOfSupportedAdvertisingSetsCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Number_Of_Supported_Advertising_Sets_Command;
}