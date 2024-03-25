using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeWriteSuggestedDefaultDataLengthCommand(ushort SuggestedMaxTxOctets,
    ushort SuggestedMaxTxTime) : IHciCommand<HciLeWriteSuggestedDefaultDataLengthCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Write_Suggested_Default_Data_Length;
    public HciLeWriteSuggestedDefaultDataLengthCommand GetThis() => this;
}