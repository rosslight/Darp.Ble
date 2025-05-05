using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetRandomAddressCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetRandomAddressResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }
}
