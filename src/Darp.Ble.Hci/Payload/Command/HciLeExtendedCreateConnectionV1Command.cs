using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeExtendedCreateConnectionV1Command : IHciCommand<HciLeExtendedCreateConnectionV1Command>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Extended_Create_ConnectionV1;

    public required byte InitiatorFilterPolicy { get; init; }
    public required byte OwnAddressType { get; init; }
    public required byte PeerAddressType { get; init; }
    public required DeviceAddress PeerAddress { get; init; }
    public required byte InitiatingPhys { get; init; }
    public required ushort ScanInterval { get; init; }
    public required ushort ScanWindow { get; init; }
    public required ushort ConnectionIntervalMin { get; init; }
    public required ushort ConnectionIntervalMax { get; init; }
    public required ushort MaxLatency { get; init; }
    public required ushort SupervisionTimeout { get; init; }
    public required ushort MinCeLength { get; init; }
    public required ushort MaxCeLength { get; init; }

    public HciLeExtendedCreateConnectionV1Command GetThis() => this;
}