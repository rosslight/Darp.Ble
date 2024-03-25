using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeEnhancedConnectionCompleteV1Event
    : IHciLeMetaEvent<HciLeEnhancedConnectionCompleteV1Event>, IDefaultDecodable<HciLeEnhancedConnectionCompleteV1Event>
{
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1;

    public required HciLeMetaSubEventType SubEventCode { get; init; }

    public required HciCommandStatus Status { get; init; }
    public required ushort ConnectionHandle { get; init; }
    public required byte Role { get; init; }
    public required byte PeerAddressType { get; init; }
    public required DeviceAddress PeerAddress { get; init; }
    public required DeviceAddress LocalResolvablePrivateAddress { get; init; }
    public required DeviceAddress PeerResolvablePrivateAddress { get; init; }
    public required ushort ConnectionInterval { get; init; }
    public required ushort PeripheralLatency { get; init; }
    public required ushort SupervisionTimeout { get; init; }
    public required byte CentralClockAccuracy { get; init; }
}