using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetEventMaskCommand(LeEventMask Mask) : IHciCommand<HciLeSetEventMaskCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Event_Mask;
    public HciLeSetEventMaskCommand GetThis() => this;
}