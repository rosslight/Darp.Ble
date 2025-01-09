using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Payload.Command;

/// <summary>
/// The HCI_LE_Set_Extended_Advertising_Parameters command is used by the Host to set the advertising parameters
/// Produces a <see cref="HciCommandCompleteEvent{TParameters}"/> with <see cref="HciLeSetExtendedAdvertisingParametersResult"/>
/// </summary>
/// <remarks> <see href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-5f795096-84b2-6d58-5b1d-a105ac59d821"/> </remarks>
[BinaryObject]
public readonly partial record struct HciLeSetExtendedAdvertisingParametersV1Command : IHciCommand
{
    /// <inheritdoc />
    public static HciOpCode OpCode => HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_PARAMETERS_V1;
    /// <summary> Advertising_Handle Used to identify an advertising set </summary>
    /// <value> 0x00 to 0xEF </value>
    public required byte AdvertisingHandle { get; init; }
    /// <summary> Advertising_Event_Properties </summary>
    public required ushort AdvertisingEventProperties { get; init; }
    /// <summary> Primary_Advertising_Interval_Min - Minimum advertising interval for undirected and low duty cycle directed advertising. </summary>
    /// <value>
    /// Range: 0x000020 to 0xFFFFFF
    /// Time = N × 0.625 ms
    /// Time Range: 20 ms to 10,485.759375 s
    /// </value>
    public required UInt24 PrimaryAdvertisingIntervalMin { get; init; }
    /// <summary> Primary_Advertising_Interval_Max - Maximum advertising interval for undirected and low duty cycle directed advertising. </summary>
    /// <value>
    /// Range: 0x000020 to 0xFFFFFF
    /// Time = N × 0.625 ms
    /// Time Range: 20 ms to 10,485.759375 s
    /// </value>
    public required UInt24 PrimaryAdvertisingIntervalMax { get; init; }
    /// <summary> Primary_Advertising_Channel_Map </summary>
    public required byte PrimaryAdvertisingChannelMap { get; init; }
    /// <summary> Own_Address_Type </summary>
    public required byte OwnAddressType { get; init; }
    /// <summary> Peer_Address_Type </summary>
    public required byte PeerAddressType { get; init; }
    /// <summary> Peer_Address - Public Device Address, Random Device Address, Public Identity Address, or Random (static) Identity Address of the device to be connected. </summary>
    public required UInt48 PeerAddress { get; init; }
    /// <summary> Advertising_Filter_Policy </summary>
    public required byte AdvertisingFilterPolicy { get; init; }
    /// <summary> Advertising_TX_Power </summary>
    public required byte AdvertisingTxPower { get; init; }
    /// <summary> Primary_Advertising_PHY </summary>
    public required byte PrimaryAdvertisingPhy { get; init; }
    /// <summary> Secondary_Advertising_Max_Skip </summary>
    public required byte SecondaryAdvertisingMaxSkip { get; init; }
    /// <summary> Secondary_Advertising_PHY </summary>
    public required byte SecondaryAdvertisingPhy { get; init; }
    /// <summary> Advertising_SID </summary>
    public required byte AdvertisingSid { get; init; }
    /// <summary> Scan_Request_Notification_Enable </summary>
    public required byte ScanRequestNotificationEnable { get; init; }
}