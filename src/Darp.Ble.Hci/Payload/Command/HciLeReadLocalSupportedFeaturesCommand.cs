using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// This command requests page 0 of the list of the supported LE features for the Controller.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeReadLocalSupportedFeaturesResult"/>
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-17fd86f1-8ddf-4b60-d81e-e2bd121b3295 </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadLocalSupportedFeaturesCommand : IHciCommand<HciLeReadLocalSupportedFeaturesCommand>
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Local_Supported_Features;
}