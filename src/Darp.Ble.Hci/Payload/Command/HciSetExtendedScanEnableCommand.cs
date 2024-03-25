using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciSetExtendedScanEnableCommand(byte Enable,
    byte FilterDuplicates,
    ushort Duration,
    ushort Period) : IHciCommand<HciSetExtendedScanEnableCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Extended_Scan_Enable;
    public HciSetExtendedScanEnableCommand GetThis() => this;
}