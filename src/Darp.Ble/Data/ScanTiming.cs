namespace Darp.Ble.Data;

/// <summary> The scan timing. Units in [0.625 ms] </summary>
public enum ScanTiming : ushort
{
    /// <summary> The minimum scan timing of 2.5ms (4) </summary>
    MinValue = 0x0004,

    /// <summary> A scan timing of 100ms (160) </summary>
    Ms100 = 160,

    /// <summary> A scan timing of 1000ms (1600) </summary>
    Ms1000 = 1600,

    /// <summary> The maximum scan timing of 40.959s (65535) </summary>
    MaxValue = 0xFFFF,
}
