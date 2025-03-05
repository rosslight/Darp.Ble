using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Write_Suggested_Default_Data_Length command allows the Host to specify its suggested values for the Controller's maximum transmission number of payload octets and maximum packet transmission time for packets containing LL Data PDUs to be used for new connections.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeWriteSuggestedDefaultDataLengthResult"/>
/// </summary>
/// <param name="SuggestedMaxTxOctets"> The Host's suggested value for the Controller's maximum transmitted number of payload octets in LL Data PDUs to be used for new connections </param>
/// <param name="SuggestedMaxTxTime"> The Host's suggested value for the Controller's maximum packet transmission time for packets containing LL Data PDUs to be used for new connections </param>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-6d593512-df5b-dc77-e549-ec1e96db0204 </remarks>
[BinaryObject]
public readonly partial record struct HciLeWriteSuggestedDefaultDataLengthCommand(
    ushort SuggestedMaxTxOctets,
    ushort SuggestedMaxTxTime
) : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Write_Suggested_Default_Data_Length;
}
