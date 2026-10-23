namespace Darp.Ble.Gap;

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
