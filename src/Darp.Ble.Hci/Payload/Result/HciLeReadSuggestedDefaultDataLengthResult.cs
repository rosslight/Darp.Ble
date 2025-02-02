using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadSuggestedDefaultDataLengthCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeReadSuggestedDefaultDataLengthResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> The Suggested_Max_TX_Octets </summary>
    public required ushort SuggestedMaxTxOctets { get; init; }

    /// <summary> The Suggested_Max_TX_Time </summary>
    public required ushort SuggestedMaxTxTime { get; init; }
}
