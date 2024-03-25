using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciReadBdAddrResult : IDefaultDecodable<HciReadBdAddrResult>
{
    public required HciCommandStatus Status { get; init; }
    public required DeviceAddress Address { get; init; }
}