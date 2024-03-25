using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciDisconnectionCompleteEvent : IHciEvent<HciDisconnectionCompleteEvent>, IDefaultDecodable<HciDisconnectionCompleteEvent>
{
    public static HciEventCode EventCode => HciEventCode.HCI_Disconnection_Complete;
    public required ushort ConnectionHandle { get; init; }
    public required HciCommandStatus Reason { get; init; }
}