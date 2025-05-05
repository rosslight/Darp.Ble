using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetEventMaskCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetEventMaskResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }
}
