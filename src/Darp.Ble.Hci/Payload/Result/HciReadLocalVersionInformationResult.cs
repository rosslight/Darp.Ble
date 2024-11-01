using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciReadLocalVersionInformationCommand"/> </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciReadLocalVersionInformationResult : IDefaultDecodable<HciReadLocalVersionInformationResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> The HCI_Version </summary>
    public required byte HciVersion { get; init; }
    /// <summary> The HCI_Subversion </summary>
    public required ushort HciSubversion { get; init; }
    /// <summary> The LMP_Version </summary>
    public required byte LmpVersion { get; init; }
    /// <summary> The Company_Identifier </summary>
    public required ushort CompanyIdentifier { get; init; }
    /// <summary> The LMP_Subversion </summary>
    public required ushort LmpSubversion { get; init; }
}