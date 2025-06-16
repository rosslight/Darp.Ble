using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Scan_Response_Data command is used to provide scan response data used in scanning response PDUs.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetExtendedScanResponseDataResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-85d7010e-3e72-9faa-9791-65ee85e55931"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedScanResponseDataCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Extended_Scan_Response_Data;

    /// <summary> Advertising_Handle Used to identify an advertising set </summary>
    /// <value> 0x00 to 0xEF </value>
    public required byte AdvertisingHandle { get; init; }

    /// <summary> Operation </summary>
    public required byte Operation { get; init; }

    /// <summary> Fragment_Preference </summary>
    public required byte FragmentPreference { get; init; }

    /// <summary> Scan_Response_Data_Length </summary>
    public required byte ScanResponseDataLength { get; init; }

    /// <summary> Scan_Response_Data </summary>
    [BinaryElementCount(nameof(ScanResponseDataLength))]
    public required ReadOnlyMemory<byte> ScanResponseData { get; init; }
}
