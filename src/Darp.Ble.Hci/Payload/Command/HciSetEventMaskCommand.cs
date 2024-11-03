using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_Set_Event_Mask command is used to control which events are generated by the HCI for the Host.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciSetEventMaskResult"/>
/// </summary>
/// <param name="Mask"> The Event_Mask </param>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-f65458cb-06cf-778a-868e-845078cc8817 </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciSetEventMaskCommand(EventMask Mask) : IHciCommand<HciSetEventMaskCommand>
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_Set_Event_Mask;
}