using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_Reset command will reset the Controller and the Link Manager on the BR/EDR Controller or the Link Layer on an LE Controller
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciResetResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-73c47658-fb48-feb6-81f8-43f6f3e775ae </remarks>
[BinaryObject]
public readonly partial record struct HciResetCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_Reset;
}
