using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Create_Connection_Cancel command is used to cancel the HCI_LE_Create_Connection or HCI_LE_Extended_Create_Connection commands.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeCreateConnectionCancelResult"/>; a pending attempt then ends with a connection complete event
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-4f6e4a7c-b2fb-ff36-3d5f-9e1aeb1d2d0a"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeCreateConnectionCancelCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Create_Connection_Cancel;
}
