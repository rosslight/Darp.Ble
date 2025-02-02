using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Random_Address command is used by the Host to set the LE Random Device Address in the Controller.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetRandomAddressResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e9af4da8-7164-2f2f-bfa3-cbbb5f2990c9 </remarks>
/// <param name="RandomAddress"></param>
[BinaryObject]
public readonly partial record struct HciLeSetRandomAddressCommand(UInt48 RandomAddress) : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Random_Address;
}
