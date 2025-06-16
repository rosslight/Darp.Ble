using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetExtendedAdvertisingParametersV1Command"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedAdvertisingParametersResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }

    /// <summary> Selected_TX_Power </summary>
    public required sbyte SelectedTxPower { get; init; }
}
