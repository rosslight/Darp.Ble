using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Read_Suggested_Default_Data_Length command allows the Host to read the Host's suggested values (Suggested_Max_TX_Octets and Suggested_Max_TX_Time) for the Controller's maximum transmitted number of payload octets and maximum packet transmission time for packets containing LL Data PDUs to be used for new connections
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeReadSuggestedDefaultDataLengthResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-8f289944-06e2-96ae-5b43-ec0c7f8cc3e0 </remarks>
[BinaryObject]
public readonly partial record struct HciLeReadSuggestedDefaultDataLengthCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Suggested_Default_Data_Length;
}