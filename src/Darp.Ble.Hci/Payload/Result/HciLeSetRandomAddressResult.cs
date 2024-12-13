using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeSetRandomAddressCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciLeSetRandomAddressResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
}