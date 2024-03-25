using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciReadBdAddrCommand : IHciCommand<HciReadBdAddrCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Read_BD_ADDR;
    public HciReadBdAddrCommand GetThis() => this;
}