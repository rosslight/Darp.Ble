using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeDataLengthChangeEvent
    : IHciLeMetaEvent<HciLeDataLengthChangeEvent>, IDefaultDecodable<HciLeDataLengthChangeEvent>
{
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Data_Length_Change;

    public required HciLeMetaSubEventType SubEventCode { get; init; }
    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }
    public required ushort MaxTxOctets { get; init; }
    public required ushort MaxTxTime { get; init; }
    public required ushort MaxRxOctets { get; init; }
    public required ushort MaxRxTime { get; init; }
}