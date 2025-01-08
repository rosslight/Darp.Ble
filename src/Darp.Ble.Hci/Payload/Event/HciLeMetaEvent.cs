using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The LE Meta event is used to encapsulate all LE Controller specific events </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bacd71f4-fabc-238d-72ee-f9aaaf5cbf22"/>
[BinaryObject]
public readonly partial record struct HciLeMetaEvent : IHciEvent<HciLeMetaEvent>
{
    /// <inheritdoc />
    public static HciEventCode EventCode => HciEventCode.HCI_LE_Meta;

    /// <summary> The SubEventCode </summary>
    public required HciLeMetaSubEventType SubEventCode { get; init; }
}