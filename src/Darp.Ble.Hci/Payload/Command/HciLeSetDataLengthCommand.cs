using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Data_Length command allows the Host to suggest the maximum transmission payload size and maximum packet transmission time to be used for LL Data PDUs on a given connection
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetDataLengthResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-a369924a-ef02-6517-061f-b30ee2dfb45e </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetDataLengthCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Data_Length;

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> Preferred maximum number of payload octets that the local Controller should include in a single LL Data PDU on this connection./// </summary>
    public required ushort TxOctets { get; init; }

    /// <summary> Preferred maximum number of microseconds that the local Controller should use to transmit a single Link Layer packet containing an LL Data PDU on this connection. </summary>
    public required ushort TxTime { get; init; }
}
