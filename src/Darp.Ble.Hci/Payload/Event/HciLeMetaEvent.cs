using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeMetaEvent : IHciEvent<HciLeMetaEvent>, IDefaultDecodable<HciLeMetaEvent>
{
    public static HciEventCode EventCode => HciEventCode.HCI_LE_Meta;
    public required HciLeMetaSubEventType SubEventCode { get; init; }
}