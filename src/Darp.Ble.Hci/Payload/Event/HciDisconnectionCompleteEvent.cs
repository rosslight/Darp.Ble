using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_Disconnection_Complete event occurs when a connection is terminated </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-95dac1bf-cdc9-0927-5034-f8e25c62dfd0"/>
[BinaryObject]
public readonly partial record struct HciDisconnectionCompleteEvent : IHciEvent<HciDisconnectionCompleteEvent>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_Disconnection_Complete;

    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }

    /// <summary> The Reason </summary>
    public required HciCommandStatus Reason { get; init; }
}
