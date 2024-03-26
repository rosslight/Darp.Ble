namespace Darp.Ble.Data;

/// <summary>
/// The scan timing. Units in [1.6ms]
/// </summary>
public enum ScanTiming : ushort
{
    /// <summary> A scan timing of 100ms (160) </summary>
    Ms100 = 160,
    /// <summary> A scan timing of 1000ms (1600) </summary>
    Ms1000 = 1600,
    /// <summary>
    /// The maximum scan timing of 40.959s (65535)
    /// </summary>
    MaxValue = 0xFFFF,
}