using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Data_Length_Change event notifies the Host of a change to either the maximum LL Data PDU Payload length or the maximum transmission time of packets containing LL Data PDUs in either direction </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-f97f9e50-69db-5e1d-25c2-9d569b5532eb"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeDataLengthChangeEvent
    : IHciLeMetaEvent<HciLeDataLengthChangeEvent>, IDefaultDecodable<HciLeDataLengthChangeEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Data_Length_Change;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }
    /// <summary> Connection_Handle </summary>
    /// <remarks> Range: 0x0000 to 0x0EFF </remarks>
    public required ushort ConnectionHandle { get; init; }
    /// <summary> The Max_TX_Octets </summary>
    public required ushort MaxTxOctets { get; init; }
    /// <summary> The Max_TX_Time </summary>
    public required ushort MaxTxTime { get; init; }
    /// <summary> The Max_RX_Octets </summary>
    public required ushort MaxRxOctets { get; init; }
    /// <summary> The Max_RX_Time </summary>
    public required ushort MaxRxTime { get; init; }
}