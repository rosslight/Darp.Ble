using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Darp.Ble.Connections;
using Darp.Ble.Gatt;

namespace Darp.Ble.Diagnostics;

/// <summary> The core's activity source, meter, instruments and tag keys; names are part of the API </summary>
internal static class Telemetry
{
    private static readonly string? Version = typeof(Telemetry)
        .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;

    public static ActivitySource Source { get; } = new(BleTelemetry.SourceName, Version);

    public static Meter Meter { get; } = new(BleTelemetry.MeterName, Version);

    public static Histogram<double> RequestDuration { get; } =
        Meter.CreateHistogram<double>(
            "darp.ble.gatt.client.request.duration",
            unit: "s",
            description: "How long GATT requests of the central side take, from being queued to their end"
        );

    public static Counter<long> Dropped { get; } =
        Meter.CreateCounter<long>(
            "darp.ble.queue.dropped",
            unit: "{item}",
            description: "Items a stream's queue dropped or could not take"
        );

    public static UpDownCounter<long> Connections { get; } =
        Meter.CreateUpDownCounter<long>(
            "darp.ble.connection.count",
            unit: "{connection}",
            description: "Open connections"
        );

    public const string BackendTag = "darp.ble.backend";
    public const string OperationTag = "darp.ble.operation";
    public const string OutcomeTag = "darp.ble.outcome";
    public const string UuidTag = "darp.ble.uuid";
    public const string AddressTag = "darp.ble.address";
    public const string QueueTag = "darp.ble.queue";
    public const string RoleTag = "darp.ble.role";

    public const string Success = "success";
    public const string Cancelled = "cancelled";
    public const string AttError = "att_error";
    public const string Disconnected = "disconnected";
    public const string Error = "error";

    /// <summary> A value as a tag shows it, culture-independent </summary>
    public static string Text(object value) => FormattableString.Invariant($"{value}");

    /// <summary> The outcome of an operation that failed </summary>
    public static string OutcomeOf(Exception exception) =>
        exception switch
        {
            OperationCanceledException => Cancelled,
            GattException { Kind: GattErrorKind.AttError } => AttError,
            BleDisconnectedException => Disconnected,
            _ => Error,
        };

    /// <summary> Ends a span with its outcome; cancellation is not an error </summary>
    /// <param name="activity"> The span, null when nobody listens </param>
    /// <param name="outcome"> The outcome </param>
    /// <param name="exception"> The failure, if any </param>
    /// <param name="settings"> What the span may show; peer operations pass theirs, so a message naming the peer can be left out </param>
    public static void Finish(
        Activity? activity,
        string outcome,
        Exception? exception = null,
        DiagnosticsSettings? settings = null
    )
    {
        if (activity is null)
            return;
        activity.SetTag(OutcomeTag, outcome);
        if (outcome is not (Success or Cancelled))
        {
            string? description =
                exception is null ? null
                : settings is null ? exception.Message
                : settings.Describe(exception);
            activity.SetStatus(ActivityStatusCode.Error, description);
        }
        activity.Stop();
    }
}
