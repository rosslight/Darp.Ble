using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeWriteSuggestedDefaultDataLengthCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeWriteSuggestedDefaultDataLengthResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }
}
