namespace Darp.Ble.Data;

/// <summary> The connection timing. Units in [1.25 ms] </summary>
public enum ConnectionTiming : ushort
{
    /// <summary> The minimum connection timing of 7.5ms (6) </summary>
    MinValue = 0x0006,
    /// <summary> A connection timing of 100ms (80) </summary>
    Ms100 = 80,
    /// <summary> A connection timing of 1000ms (800) </summary>
    Ms1000 = 800,
    /// <summary> The maximum connection timing of 4s (3200) </summary>
    MaxValue = 0x0C80,
}