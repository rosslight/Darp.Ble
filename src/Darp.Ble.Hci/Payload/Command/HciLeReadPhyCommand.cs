using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Read_PHY command reads the current PHYs for the connection identified by the Connection_Handle.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeReadPhyResult"/>.
/// OpCode: <see cref="HciOpCode.HCI_LE_READ_PHY"/>.
/// </summary>
/// <remarks>https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bf5bec23-cd60-0648-9342-fa6d0886de40</remarks>
[BinaryObject]
public readonly partial record struct HciLeReadPhyCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_READ_PHY;

    /// <summary> Connection_Handle (Range: 0x0000 to 0x0EFF) </summary>
    public required ushort ConnectionHandle { get; init; }
}
