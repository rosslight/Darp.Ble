using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciNumberOfCompletedPacketsInfo : IDefaultDecodable<HciNumberOfCompletedPacketsInfo>
{
    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }
    public required ushort NumCompletedPackets { get; init; }
}