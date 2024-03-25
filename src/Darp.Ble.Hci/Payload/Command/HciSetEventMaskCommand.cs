using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[Flags]
public enum EventMask : ulong
{
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciSetEventMaskCommand(EventMask Mask) : IHciCommand<HciSetEventMaskCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Set_Event_Mask;
    public HciSetEventMaskCommand GetThis() => this;
}