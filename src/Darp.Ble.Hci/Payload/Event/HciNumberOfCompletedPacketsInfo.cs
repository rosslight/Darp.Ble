using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciNumberOfCompletedPacketsInfo : IDefaultDecodable<HciNumberOfCompletedPacketsInfo>
{
    public required ushort ConnectionHandle { get; init; }
    public required ushort NumCompletedPackets { get; init; }
}