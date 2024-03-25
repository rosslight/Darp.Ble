using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeReadBufferSizeResultV1 : IDefaultDecodable<HciLeReadBufferSizeResultV1>
{
    public required HciCommandStatus Status { get; init; }
    public required ushort LeAclDataPacketLength { get; init; }
    public required byte TotalNumLeAclDataPackets { get; init; }
}