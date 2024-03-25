using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciDisconnectCommand : IHciCommand<HciDisconnectCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Disconnect;

    public required ushort ConnectionHandle { get; init; }
    public required HciCommandStatus Reason { get; init; }

    public HciDisconnectCommand GetThis() => this;
}