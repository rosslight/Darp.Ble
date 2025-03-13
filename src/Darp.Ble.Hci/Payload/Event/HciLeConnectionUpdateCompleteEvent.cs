using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Data_Length_Change event notifies the Host of a change to either the maximum LL Data PDU Payload length or the maximum transmission time of packets containing LL Data PDUs in either direction </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-f97f9e50-69db-5e1d-25c2-9d569b5532eb"/>
[BinaryObject]
public readonly partial record struct HciLeConnectionUpdateCompleteEvent
    : IHciLeMetaEvent<HciLeConnectionUpdateCompleteEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Connection_Update_Complete;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> The Connection_Interval </summary>
    public required ushort ConnectionInterval { get; init; }

    /// <summary> The Peripheral_Latency </summary>
    public required ushort PeripheralLatency { get; init; }

    /// <summary> The Supervision_Timeout </summary>
    public required ushort SupervisionTimeout { get; init; }
}
