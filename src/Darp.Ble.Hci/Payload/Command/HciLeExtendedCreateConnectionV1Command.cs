using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Extended_Create_Connection command is used to create an ACL connection, with the local device in the Central role, to a connectable advertiser.
/// The command is also used to create an ACL connection between a periodic advertiser and a synchronized device.
/// Produces a <see cref="HciCommandStatusEvent"/> and HCI_LE_Enhanced_Connection_Complete event
/// </summary>
/// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-5b224d07-6721-e019-2b95-3d7b8f2e43c5 </remarks>
[BinaryObject]
public readonly partial record struct HciLeExtendedCreateConnectionV1Command : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Extended_Create_ConnectionV1;

    /// <summary> The Initiator_Filter_Policy parameter is used to determine whether the Filter Accept List is used and whether to process decision PDUs and other advertising PDUs </summary>
    public required byte InitiatorFilterPolicy { get; init; }
    /// <summary> The Own_Address_Type parameter indicates the type of address being used in the connection request packets. </summary>
    public required byte OwnAddressType { get; init; }
    /// <summary> The Peer_Address_Type parameter indicates the type of address used in the connectable advertisement sent by the peer. </summary>
    public required byte PeerAddressType { get; init; }
    /// <summary> The Peer_Address parameter </summary>
    public required DeviceAddress PeerAddress { get; init; }
    /// <summary> The Initiating_PHYs parameter indicates the PHY(s) on which the advertising packets should be received on the primary advertising physical channel and the PHYs for which connection parameters have been specified </summary>
    public required byte InitiatingPhys { get; init; }
    /// <summary> The Scan_Interval[i] and Scan_Window[i] parameters are recommendations from the Host on how long (Scan_Window[i]) and how frequently (Scan_Interval[i]) the Controller should scan </summary>
    public required ushort ScanInterval { get; init; }
    /// <summary> The Scan_Interval[i] and Scan_Window[i] parameters are recommendations from the Host on how long (Scan_Window[i]) and how frequently (Scan_Interval[i]) the Controller should scan </summary>
    public required ushort ScanWindow { get; init; }
    /// <summary> The Connection_Interval_Min[i] and Connection_Interval_Max[i] parameters define the minimum and maximum allowed connection interval </summary>
    public required ushort ConnectionIntervalMin { get; init; }
    /// <summary> The Connection_Interval_Min[i] and Connection_Interval_Max[i] parameters define the minimum and maximum allowed connection interval </summary>
    public required ushort ConnectionIntervalMax { get; init; }
    /// <summary> The Max_Latency[i] parameter defines the maximum allowed Peripheral latency </summary>
    public required ushort MaxLatency { get; init; }
    /// <summary> The Supervision_Timeout[i] parameter defines the link supervision timeout for the connection </summary>
    public required ushort SupervisionTimeout { get; init; }
    /// <summary> The Min_CE_Length[i] and Max_CE_Length[i] parameters provide the Controller with the expected minimum and maximum length of the connection events </summary>
    public required ushort MinCeLength { get; init; }
    /// <summary> The Min_CE_Length[i] and Max_CE_Length[i] parameters provide the Controller with the expected minimum and maximum length of the connection events </summary>
    public required ushort MaxCeLength { get; init; }
}