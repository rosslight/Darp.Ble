using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeReadSuggestedDefaultDataLengthResult : IDefaultDecodable<HciLeReadSuggestedDefaultDataLengthResult>
{
    public required HciCommandStatus Status { get; init; }
    public required ushort SuggestedMaxTxOctets { get; init; }
    public required ushort SuggestedMaxTxTime { get; init; }
}