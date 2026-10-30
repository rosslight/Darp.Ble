namespace Darp.Ble.Gap;

/// <summary> The radio settings of scans. They belong to the observer and apply to every scan running on it </summary>
public sealed record BleScanParameters
{
    /// <summary> Whether the radio asks advertisers for their scan response </summary>
    public ScanType ScanType { get; init; } = ScanType.Passive;

    /// <summary> How often the radio starts listening. The backend rounds it to the controller's units </summary>
    public TimeSpan ScanInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary> How long the radio listens each time; at most <see cref="ScanInterval"/> </summary>
    public TimeSpan ScanWindow { get; init; } = TimeSpan.FromMilliseconds(100);

    internal void Validate(string parameterName)
    {
        if (ScanInterval <= TimeSpan.Zero || ScanWindow <= TimeSpan.Zero)
            throw new ArgumentException("The scan interval and window must be positive", parameterName);
        if (ScanWindow > ScanInterval)
            throw new ArgumentException("The scan window must not be longer than the scan interval", parameterName);
    }
}
