using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Connection_Update_Complete event is used to indicate that the Connection Update procedure has completed. </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-0422d5f6-dcf2-88f9-be9c-d6de51256cba"/>
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
