namespace Darp.Ble.Data;

/// <summary> The scan type </summary>
public enum ScanType
{
    /// <summary> Passive scanning </summary>
    /// <remarks> When starting a scan with <see cref="Passive"/>, a scan will be started but no scan request packets will be send </remarks>
    Passive = 0,

    /// <summary> Active scanning </summary>
    /// <remarks>
    /// When starting a scan with <see cref="Active"/>, a scan will be started. Scan request packets will be sent if the remote device advertises to be <see cref="BleEventType.Scannable"/>
    /// </remarks>
    Active = 1,
}
