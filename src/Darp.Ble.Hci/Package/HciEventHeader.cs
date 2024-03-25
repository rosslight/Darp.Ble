using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci.Package;

[StructLayout(LayoutKind.Sequential)]
public readonly struct HciEventHeader
{
    public required HciEventCode EventType { get; init; }
    public required byte PayloadLength { get; init; }
}