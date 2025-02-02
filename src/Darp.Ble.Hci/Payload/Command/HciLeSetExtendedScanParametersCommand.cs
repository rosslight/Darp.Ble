using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Scan_Parameters command is used to set the extended scan parameters to be used on the advertising physical channels.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciSetExtendedScanParametersResult"/>
/// </summary>
/// <param name="OwnAddressType"> The Own_Address_Type parameter indicates the type of address being used in the scan request packets. </param>
/// <param name="ScanningFilterPolicy"> The Scanning_Filter_Policy </param>
/// <param name="ScanPhys"> The Scanning_PHYs parameter indicates the PHY(s) on which the advertising packets should be received on the primary advertising physical channel </param>
/// <param name="ScanType"> The Scan_Type[i] parameter specifies the type of scan to perform. </param>
/// <param name="ScanInterval"> The Scan_Interval[i] and Scan_Window[i] parameters are recommendations from the Host on how long (Scan_Window[i]) and how frequently (Scan_Interval[i]) the Controller should scan </param>
/// <param name="ScanWindow"> The Scan_Interval[i] and Scan_Window[i] parameters are recommendations from the Host on how long (Scan_Window[i]) and how frequently (Scan_Interval[i]) the Controller should scan </param>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-f5242ce9-9acb-fff4-3e8f-8f6465b2f24c </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedScanParametersCommand(byte OwnAddressType,
    byte ScanningFilterPolicy,
    byte ScanPhys,
    byte ScanType,
    ushort ScanInterval,
    ushort ScanWindow) : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Extended_Scan_Parameters;
}