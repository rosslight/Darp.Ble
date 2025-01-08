using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Enhanced_Connection_Complete event indicates to both of the Hosts forming the connection that a new connection has been created </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-418db27a-3baa-9e9f-0be8-45be92f57fcb"/>
[BinaryObject]
public readonly partial record struct HciLeEnhancedConnectionCompleteV1Event
    : IHciLeMetaEvent<HciLeEnhancedConnectionCompleteV1Event>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> The Status </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }
    /// <summary> The Role </summary>
    public required byte Role { get; init; }
    /// <summary> The Peer_Address_Type </summary>
    public required byte PeerAddressType { get; init; }
    /// <summary> The Peer_Address </summary>
    public required DeviceAddress PeerAddress { get; init; }
    /// <summary> The Local_Resolvable_Private_Address </summary>
    public required DeviceAddress LocalResolvablePrivateAddress { get; init; }
    /// <summary> The Peer_Resolvable_Private_Address </summary>
    public required DeviceAddress PeerResolvablePrivateAddress { get; init; }
    /// <summary> The Connection_Interval </summary>
    public required ushort ConnectionInterval { get; init; }
    /// <summary> The Peripheral_Latency </summary>
    public required ushort PeripheralLatency { get; init; }
    /// <summary> The Supervision_Timeout </summary>
    public required ushort SupervisionTimeout { get; init; }
    /// <summary> The Central_Clock_Accuracy </summary>
    public required byte CentralClockAccuracy { get; init; }
}