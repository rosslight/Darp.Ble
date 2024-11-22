using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciSetEventMaskCommand"/> </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciSetEventMaskResult : IDefaultDecodable<HciSetEventMaskResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
}