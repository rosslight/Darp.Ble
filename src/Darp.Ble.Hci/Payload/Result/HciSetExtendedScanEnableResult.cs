using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetExtendedScanEnableCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciSetExtendedScanEnableResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }
}
