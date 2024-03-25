using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadBufferSizeCommandV1Command : IHciCommand<HciLeReadBufferSizeCommandV1Command>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Buffer_Size_V1;
    public HciLeReadBufferSizeCommandV1Command GetThis() => this;
}