namespace Darp.Ble.Data;

/// <summary> The parameters used when configuring a ble scan </summary>
public sealed record BleObservationParameters
{
    /// <summary> The scan type </summary>
    public ScanType ScanType { get; init; } = ScanType.Passive;

    /// <summary> The scan interval </summary>
    public ScanTiming ScanInterval { get; init; } = ScanTiming.Ms100;

    /// <summary> The scan window </summary>
    public ScanTiming ScanWindow { get; init; } = ScanTiming.Ms100;
}
