using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// This command reads the list of HCI commands supported for the local Controller.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciReadLocalSupportedFeaturesResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-9dda79e8-0d1d-40a4-a314-502252127b27 </remarks>
[BinaryObject]
public readonly partial record struct HciReadLocalSupportedFeaturesCommand : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_Read_Local_Supported_Features;
}
