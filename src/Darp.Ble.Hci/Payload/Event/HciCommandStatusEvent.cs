using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Command_Status event is used to indicate that the command described by the Command_Opcode parameter has been received, and that the Controller is currently performing the task for this command </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-3966af71-7053-31c7-3e0a-0d786e802744"/>
[BinaryObject]
public readonly partial record struct HciCommandStatusEvent : IHciEvent<HciCommandStatusEvent>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_Command_Status;

    /// <summary> The Status </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> The Number of HCI Command packets which are allowed to be sent to the Controller from the Host. </summary>
    public required byte NumHciCommandPackets { get; init; }

    /// <summary> The Command_Opcode </summary>
    public required HciOpCode CommandOpCode { get; init; }
}
