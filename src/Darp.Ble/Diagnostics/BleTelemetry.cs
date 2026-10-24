namespace Darp.Ble.Diagnostics;

/// <summary> The names to subscribe to the library's telemetry, e.g. with OpenTelemetry's <c>AddSource</c> and <c>AddMeter</c> </summary>
/// <remarks> docs/telemetry.md lists every span, tag, metric and event id; that list is part of the API </remarks>
public static class BleTelemetry
{
    /// <summary> The activity source of the core's spans </summary>
    public const string SourceName = "Darp.Ble";

    /// <summary> The meter of the core's metrics </summary>
    public const string MeterName = "Darp.Ble";
}
