using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetDataLengthCommand : IHciCommand<HciLeSetDataLengthCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Data_Length;
    public required ushort ConnectionHandle { get; init; }
    public required ushort TxOctets { get; init; }
    public required ushort TxTime { get; init; }
    public HciLeSetDataLengthCommand GetThis() => this;
}