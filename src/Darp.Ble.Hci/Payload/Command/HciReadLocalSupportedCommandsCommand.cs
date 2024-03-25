using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

public readonly record struct HciReadLocalSupportedCommandsCommand : IHciCommand<HciReadLocalSupportedCommandsCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Read_Local_Supported_Commands;
    public HciReadLocalSupportedCommandsCommand GetThis() => this;
}