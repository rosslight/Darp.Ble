using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// This command is used to read the maximum size of the data portion of ACL data packets and isochronous data packets sent from the Host to the Controller.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeReadBufferSizeResultV1"/>
/// </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-de6d1e19-fa7a-4f93-c178-6ada8e837ade"/>
[BinaryObject]
public readonly partial record struct HciLeReadBufferSizeCommandV1 : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Buffer_Size_V1;
}
