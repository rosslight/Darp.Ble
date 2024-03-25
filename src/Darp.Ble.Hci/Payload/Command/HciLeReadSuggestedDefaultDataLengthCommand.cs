using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadSuggestedDefaultDataLengthCommand : IHciCommand<HciLeReadSuggestedDefaultDataLengthCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Suggested_Default_Data_Length;
    public HciLeReadSuggestedDefaultDataLengthCommand GetThis() => this;
}