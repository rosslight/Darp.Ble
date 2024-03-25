using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciCommandStatusEvent : IHciEvent<HciCommandStatusEvent>, IDefaultDecodable<HciCommandStatusEvent>
{
    public static HciEventCode EventCode => HciEventCode.HCI_Command_Status;

    public required HciCommandStatus Status { get; init; }
    public required byte NumHciCommandPackets { get; init; }
    public required HciOpCode CommandOpCode { get; init; }
}