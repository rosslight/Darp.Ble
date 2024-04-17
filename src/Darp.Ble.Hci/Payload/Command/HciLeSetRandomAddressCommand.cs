using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetRandomAddressCommand(DeviceAddress RandomAddress)
    : IHciCommand<HciLeSetRandomAddressCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Random_Address;
    public HciLeSetRandomAddressCommand GetThis() => this;
}