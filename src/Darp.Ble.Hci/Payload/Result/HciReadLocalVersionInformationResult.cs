using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciReadLocalVersionInformationResult : IDefaultDecodable<HciReadLocalVersionInformationResult>
{
    public required HciCommandStatus Status { get; init; }
    public required byte HciVersion { get; init; }
    public required ushort HciSubversion { get; init; }
    public required byte LmpVersion { get; init; }
    public required ushort CompanyIdentifier { get; init; }
    public required ushort LmpSubversion { get; init; }
}