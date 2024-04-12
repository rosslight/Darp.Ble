namespace Darp.Ble.Data;

/// <summary> The parameters used when configuring a ble scan </summary>
public sealed record BleScanParameters
{
    /// <summary> The scan type </summary>
    public ScanType ScanType { get; init; } = ScanType.Passive;
    /// <summary> The scan interval </summary>
    public ScanTiming ScanInterval { get; init; } = ScanTiming.Ms100;
    /// <summary> The scan window </summary>
    public ScanTiming ScanWindow { get; init; } = ScanTiming.Ms100;
}

/// <summary> The connection timing. Units in [1.25 ms] </summary>
public enum ConnectionTiming
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

/// <summary> The parameters used when configuring a ble scan </summary>
public sealed record BleConnectionParameters
{
    /// <summary> The connection interval </summary>
    public ConnectionTiming ConnectionInterval { get; init; } = ConnectionTiming.Ms100;
}