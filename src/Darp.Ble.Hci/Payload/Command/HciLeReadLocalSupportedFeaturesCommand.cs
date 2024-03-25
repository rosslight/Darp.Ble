using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadLocalSupportedFeaturesCommand : IHciCommand<HciLeReadLocalSupportedFeaturesCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Read_Local_Supported_Features;
    public HciLeReadLocalSupportedFeaturesCommand GetThis() => this;
}