using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciResetCommand : IHciCommand<HciResetCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Reset;
    public HciResetCommand GetThis() => this;
}