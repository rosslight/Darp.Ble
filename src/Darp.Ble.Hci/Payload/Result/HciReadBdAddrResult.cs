using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciReadBdAddrCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciReadBdAddrResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> The BD_ADDR </summary>
    public required DeviceAddress Address { get; init; }
}