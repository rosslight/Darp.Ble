using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Advertising_Set_Terminated event indicates that the Controller has terminated advertising in the advertising sets specified by the Advertising_Handle parameter. </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-7cdd2597-da9d-e4b9-1cc6-cf10dd3a7ef3"/>
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
