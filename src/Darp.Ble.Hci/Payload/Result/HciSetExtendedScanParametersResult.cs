using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciSetExtendedScanParametersResult : IDefaultDecodable<HciSetExtendedScanParametersResult>
{
    public required HciCommandStatus Status { get; init; }
}