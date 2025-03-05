namespace Darp.Ble.Data;

public enum AdvertisingFilterPolicy
{
    /// <summary> Process scan and connection requests from all devices (i.e., the Filter Accept List is not in use) </summary>
    AcceptAllConnectionsAndScanRequests = 0x00,

    /// <summary> Process connection requests from all devices and scan requests only from devices that are in the Filter Accept List. </summary>
    AcceptAllConnectionsAndKnownScanRequests = 0x01,

    /// <summary> Process scan requests from all devices and connection requests only from devices that are in the Filter Accept List. </summary>
    AcceptKnownConnectionsAndAllScanRequests = 0x02,

    /// <summary> Process scan and connection requests only from devices in the Filter Accept List. </summary>
    AcceptKnownConnectionsAndScanRequests = 0x03,
}

/// <summary> Advertising channels </summary>
[Flags]
public enum AdvertisingChannelMap : byte
{
    /// <summary> The advertising channel 37 </summary>
    Channel37 = 1 << 0,

    /// <summary> The advertising channel 38 </summary>
    Channel38 = 1 << 1,

    /// <summary> The advertising channel 39 </summary>
    Channel39 = 1 << 2,
}

/// <summary> Advertisement parameters to configure advertisement </summary>
public sealed class AdvertisingParameters
{
    /// <summary> The default advertising parameters </summary>
    public static AdvertisingParameters Default { get; } = new();

    /// <summary> The <see cref="BleEventType"/> to be used when sending the advertisement </summary>
    public BleEventType Type { get; init; } = BleEventType.AdvNonConnInd;

    public ScanTiming MinPrimaryAdvertisingInterval { get; init; } = ScanTiming.Ms1000;
    public ScanTiming MaxPrimaryAdvertisingInterval { get; init; } = ScanTiming.Ms1000;

    public AdvertisingChannelMap PrimaryAdvertisingChannelMap { get; init; } =
        AdvertisingChannelMap.Channel37 | AdvertisingChannelMap.Channel38 | AdvertisingChannelMap.Channel39;

    /// <summary> The address of the peer device, if <see cref="BleEventType.Directed"/> advertising is selected in the <see cref="Type"/> </summary>
    public BleAddress? PeerAddress { get; init; }

    /// <summary>
    /// The Advertising_TX_Power parameter indicates the maximum power level at which the advertising packets are to be transmitted on the advertising physical channels.
    /// The Controller shall choose a power level lower than or equal to the one specified by the Host
    /// </summary>
    public TxPowerLevel AdvertisingTxPower { get; init; } = TxPowerLevel.NotAvailable;

    /// <summary> The filter policy </summary>
    public AdvertisingFilterPolicy FilterPolicy { get; init; } =
        AdvertisingFilterPolicy.AcceptAllConnectionsAndScanRequests;

    /// <summary> The primary physical </summary>
    /// <value> Only <see cref="Physical.Le1M"/> and <see cref="Physical.LeCoded"/> are allowed </value>
    public Physical PrimaryPhy { get; init; } = Physical.Le1M;

    /// <summary> The advertising SId </summary>
    public AdvertisingSId AdvertisingSId { get; init; }
}
