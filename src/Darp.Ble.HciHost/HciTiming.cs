namespace Darp.Ble.HciHost;

/// <summary> Converts timings to the controller's units, rounded to the nearest unit (D15) </summary>
internal static class HciTiming
{
    /// <summary> Scan interval and window: 0.625 ms units, 2.5 ms to 40.959 s </summary>
    public static ushort ToScanUnits(TimeSpan value, string name) =>
        (ushort)ToUnits(value, TimeSpan.FromMilliseconds(0.625), 0x0004, 0xFFFF, name);

    /// <summary> Primary advertising interval: 0.625 ms units, 20 ms to 10485.759 s </summary>
    public static uint ToAdvertisingUnits(TimeSpan value, string name) =>
        (uint)ToUnits(value, TimeSpan.FromMilliseconds(0.625), 0x000020, 0xFFFFFF, name);

    /// <summary> Connection interval: 1.25 ms units, 7.5 ms to 4 s </summary>
    public static ushort ToConnectionIntervalUnits(TimeSpan value, string name) =>
        (ushort)ToUnits(value, TimeSpan.FromMilliseconds(1.25), 0x0006, 0x0C80, name);

    /// <summary> Supervision timeout: 10 ms units, 100 ms to 32 s </summary>
    public static ushort ToSupervisionTimeoutUnits(TimeSpan value, string name) =>
        (ushort)ToUnits(value, TimeSpan.FromMilliseconds(10), 0x000A, 0x0C80, name);

    private static long ToUnits(TimeSpan value, TimeSpan unit, long min, long max, string name)
    {
        long units = (long)Math.Round(value / unit, MidpointRounding.AwayFromZero);
        if (units < min || units > max)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                $"The controller takes {name} between {unit * min} and {unit * max}"
            );
        }
        return units;
    }
}
