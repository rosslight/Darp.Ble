using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciReadLocalVersionInformationCommand"/> </summary>
[BinaryObject]
public readonly partial record struct HciReadLocalVersionInformationResult
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> The HCI_Version </summary>
    public required CoreVersion HciVersion { get; init; }

    /// <summary> The HCI_Subversion </summary>
    public required ushort HciSubversion { get; init; }

    /// <summary> The LMP_Version </summary>
    public required byte LmpVersion { get; init; }

    /// <summary> The Company_Identifier </summary>
    public required ushort CompanyIdentifier { get; init; }

    /// <summary> The LMP_Subversion </summary>
    public required ushort LmpSubversion { get; init; }
}
