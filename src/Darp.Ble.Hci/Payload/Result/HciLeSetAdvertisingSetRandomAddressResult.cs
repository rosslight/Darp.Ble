using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetAdvertisingSetRandomAddressCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetAdvertisingSetRandomAddressResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
}