using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_PHY_Update_Complete event is used to indicate that the Controller has changed the transmitter PHY or receiver PHY in use </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bacd71f4-fabc-238d-72ee-f9aaaf5cbf22"/>
/// <remarks> 7.7.65.12 </remarks>
[BinaryObject]
public readonly partial record struct HciLePhyUpdateCompleteEvent : IHciLeMetaEvent<HciLePhyUpdateCompleteEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_PHY_Update_Complete;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> The Status </summary>
    public required HciCommandStatus Status { get; init; }

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> The TX_PHY </summary>
    public required byte TxPhy { get; init; }

    /// <summary> The RX_PHY </summary>
    public required byte RxPhy { get; init; }
}
