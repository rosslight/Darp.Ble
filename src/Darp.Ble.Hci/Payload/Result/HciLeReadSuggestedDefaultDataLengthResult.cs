using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadSuggestedDefaultDataLengthCommand"/> </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadSuggestedDefaultDataLengthResult : IDefaultDecodable<HciLeReadSuggestedDefaultDataLengthResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> The Suggested_Max_TX_Octets </summary>
    public required ushort SuggestedMaxTxOctets { get; init; }
    /// <summary> The Suggested_Max_TX_Time </summary>
    public required ushort SuggestedMaxTxTime { get; init; }
}