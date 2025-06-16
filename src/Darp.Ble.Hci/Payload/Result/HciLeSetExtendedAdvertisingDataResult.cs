using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetExtendedAdvertisingDataCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedAdvertisingDataResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }
}
