using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

public readonly record struct HciReadLocalVersionInformationCommand : IHciCommand<HciReadLocalVersionInformationCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_Read_Local_Version_Information;
    public HciReadLocalVersionInformationCommand GetThis() => this;
}