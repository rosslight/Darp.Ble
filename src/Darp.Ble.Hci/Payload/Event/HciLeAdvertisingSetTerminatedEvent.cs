using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Data_Length_Change event notifies the Host of a change to either the maximum LL Data PDU Payload length or the maximum transmission time of packets containing LL Data PDUs in either direction </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-f97f9e50-69db-5e1d-25c2-9d569b5532eb"/>
[BinaryObject]
public readonly partial record struct HciLeAdvertisingSetTerminatedEvent
    : IHciLeMetaEvent<HciLeAdvertisingSetTerminatedEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Advertising_Set_Terminated;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> The status. If 0x00, the Advertising successfully ended with a connection being created </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> The Advertising_Handle </summary>
    public required byte AdvertisingHandle { get; init; }

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> The Num_Completed_Extended_Advertising_Events </summary>
    public required byte NumCompletedExtendedAdvertisingEvents { get; init; }
}
