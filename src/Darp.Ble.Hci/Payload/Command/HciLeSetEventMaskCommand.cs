using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Event_Mask command is used to control which LE events are generated by the HCI for the Host.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetEventMaskResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-cefc532c-3752-3f40-b5c1-91070b4dfef8 </remarks>
/// <param name="Mask"> The LE_Event_Mask </param>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetEventMaskCommand(LeEventMask Mask) : IHciCommand<HciLeSetEventMaskCommand>
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Event_Mask;
}