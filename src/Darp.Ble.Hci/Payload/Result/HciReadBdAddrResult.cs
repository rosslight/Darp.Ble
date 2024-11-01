using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciReadBdAddrCommand"/> </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciReadBdAddrResult : IDefaultDecodable<HciReadBdAddrResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> The BD_ADDR </summary>
    public required DeviceAddress Address { get; init; }
}