using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeRemoveAdvertisingSetCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeRemoveAdvertisingSetResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
}