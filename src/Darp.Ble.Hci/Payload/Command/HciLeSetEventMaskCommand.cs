using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[Flags]
public enum LeEventMask : ulong
{
    LeConnectionCompleteEvent = 1 << 0,
    LeAdvertisingReportEvent = 1 << 1,
    LeConnectionUpdateCompleteEvent = 1 << 2,
    LeReadRemoteFeaturesCompleteEvent = 1 << 3,
    LeLongTermKeyRequestEvent = 1 << 4,
    LeRemoteConnectionParameterRequestEvent = 1 << 5,
    LeDataLengthChangeEvent = 1 << 6,
    LeReadLocalP256PublicKeyCompleteEvent = 1 << 7,
    LeGenerateDhKeyCompleteEvent = 1 << 8,
    LeEnhancedConnectionCompleteEventV1 = 1 << 9,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetEventMaskCommand(LeEventMask Mask) : IHciCommand<HciLeSetEventMaskCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Event_Mask;
    public HciLeSetEventMaskCommand GetThis() => this;
}