using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary>Represents configuration parameters for BLE advertising.</summary>
public sealed record AdvertisingParameters
{
    /// <summary>Gets the default advertising parameters.</summary>
    public static AdvertisingParameters Default { get; } = new();

    /// <summary>Gets the advertising event type to use.</summary>
    public BleEventType Type { get; init; } = BleEventType.AdvNonConnInd;

    /// <summary>Gets the minimum interval between primary advertising events. The backend rounds it to the controller's units.</summary>
    public TimeSpan MinPrimaryAdvertisingInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets the maximum interval between primary advertising events; at least <see cref="MinPrimaryAdvertisingInterval"/>.</summary>
    public TimeSpan MaxPrimaryAdvertisingInterval { get; init; } = TimeSpan.FromSeconds(1);

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

    internal void Validate(string parameterName)
    {
        if (MinPrimaryAdvertisingInterval <= TimeSpan.Zero)
            throw new ArgumentException("The advertising interval must be positive", parameterName);
        if (MaxPrimaryAdvertisingInterval < MinPrimaryAdvertisingInterval)
            throw new ArgumentException(
                "The maximum advertising interval must not be shorter than the minimum",
                parameterName
            );
    }
}
