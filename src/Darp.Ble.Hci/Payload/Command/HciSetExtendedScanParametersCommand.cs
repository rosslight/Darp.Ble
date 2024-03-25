using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciSetExtendedScanParametersCommand(byte OwnDeviceAddress,
    byte ScanningFilterPolicy,
    byte ScanPhys,
    byte ScanType,
    ushort ScanInterval,
    ushort ScanWindow) : IHciCommand<HciSetExtendedScanParametersCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Extended_Scan_Parameters;
    public HciSetExtendedScanParametersCommand GetThis() => this;
}