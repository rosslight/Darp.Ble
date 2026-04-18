namespace Darp.Ble.Data;

/// <summary>
/// Defines how scan and connection requests are filtered while advertising.
/// </summary>
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

/// <summary>Advertising channels used on the primary advertising PHY.</summary>
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

/// <summary>Represents configuration parameters for BLE advertising.</summary>
public sealed record AdvertisingParameters
{
    /// <summary>Gets the default advertising parameters.</summary>
    public static AdvertisingParameters Default { get; } = new();

    /// <summary>Gets the advertising event type to use.</summary>
    public BleEventType Type { get; init; } = BleEventType.AdvNonConnInd;

    /// <summary>Gets the minimum interval between primary advertising events.</summary>
    public ScanTiming MinPrimaryAdvertisingInterval { get; init; } = ScanTiming.Ms1000;

    /// <summary>Gets the maximum interval between primary advertising events.</summary>
    public ScanTiming MaxPrimaryAdvertisingInterval { get; init; } = ScanTiming.Ms1000;

    /// <summary>Gets the advertising channel map for the primary PHY.</summary>
    public AdvertisingChannelMap PrimaryAdvertisingChannelMap { get; init; } =
        AdvertisingChannelMap.Channel37 | AdvertisingChannelMap.Channel38 | AdvertisingChannelMap.Channel39;

    /// <summary>Gets the peer address for directed advertising.</summary>
    public BleAddress? PeerAddress { get; init; }

    /// <summary>Gets the maximum transmit power requested for advertising packets.</summary>
    public TxPowerLevel AdvertisingTxPower { get; init; } = TxPowerLevel.NotAvailable;

    /// <summary>Gets the filter policy applied to scan and connection requests.</summary>
    public AdvertisingFilterPolicy FilterPolicy { get; init; } =
        AdvertisingFilterPolicy.AcceptAllConnectionsAndScanRequests;

    /// <summary>Gets the primary advertising PHY.</summary>
    /// <value>Only <see cref="Physical.Le1M"/> and <see cref="Physical.LeCoded"/> are valid values.</value>
    public Physical PrimaryPhy { get; init; } = Physical.Le1M;

    /// <summary>Gets the secondary advertising PHY.</summary>
    public Physical SecondaryPhy { get; init; } = Physical.Le1M;

    /// <summary>Gets the advertising SID used for extended advertising.</summary>
    public AdvertisingSId AdvertisingSId { get; init; }
}
