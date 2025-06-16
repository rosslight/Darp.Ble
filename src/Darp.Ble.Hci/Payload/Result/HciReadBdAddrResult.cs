using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciReadBdAddrCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciReadBdAddrResult : ICommandStatusResult
{
    /// <inheritdoc />
    public required HciCommandStatus Status { get; init; }

    /// <summary> The BD_ADDR </summary>
    public required UInt48 Address { get; init; }
}
