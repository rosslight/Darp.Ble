using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Scan_Enable command is used to enable or disable scanning for both legacy and extended advertising PDUs.
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciSetExtendedScanEnableResult"/>
/// </summary>
/// <param name="Enable"> The Enable parameter determines whether scanning is enabled or disabled </param>
/// <param name="FilterDuplicates"> The Filter_Duplicates parameter controls whether the Link Layer should filter out duplicate advertising reports </param>
/// <param name="Duration"> The Duration </param>
/// <param name="Period"> The Period </param>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-b96b8577-a22d-4009-cac0-dca78f793b59 </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetExtendedScanEnableCommand(byte Enable,
    byte FilterDuplicates,
    ushort Duration,
    ushort Period) : IHciCommand<HciLeSetExtendedScanEnableCommand>
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Extended_Scan_Enable;
}