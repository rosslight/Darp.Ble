using System.Runtime.InteropServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// On a BR/EDR Controller, this command reads the Bluetooth Controller address (BD_ADDR). On an LE Controller, this command shall read the Public Device Address
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciReadBdAddrResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-0b467c9e-d520-742e-8e77-4e1a3539a475 </remarks>
[BinaryObject]
public readonly partial record struct HciReadBdAddrCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_Read_BD_ADDR;
}