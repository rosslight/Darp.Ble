using System.Diagnostics;
using Darp.Ble.Diagnostics;

namespace Darp.Ble.Gap;

/// <summary> The span of one scan, from its start to its end; it never becomes current, so the consumer's work is no child of it </summary>
internal sealed class ScanSpan
{
    private readonly Activity _activity;
    private Exception? _error;

    private ScanSpan(Activity activity)
    {
        _activity = activity;
    }

    /// <summary> Starts the span, or returns null when nobody listens </summary>
    public static ScanSpan? Start(string backend)
    {
        Activity? previous = Activity.Current;
        Activity? activity = Telemetry.Source.StartActivity("Scan", ActivityKind.Consumer);
        if (activity is null)
            return null;
        Activity.Current = previous;
        if (activity.IsAllDataRequested)
            activity.SetTag(Telemetry.BackendTag, backend);
        return new ScanSpan(activity);
    }

    /// <summary> Records why the scan failed; a later failure, e.g. stopping the radio, replaces an earlier cancellation </summary>
    public void Fail(Exception error)
    {
        if (_error is null or OperationCanceledException)
            _error = error;
    }

    /// <summary> Ends the span: an error ends it with its outcome, a cancelled scan as cancelled, any other end successfully </summary>
    public void End(bool cancelled)
    {
        string outcome =
            _error is not null ? Telemetry.OutcomeOf(_error)
            : cancelled ? Telemetry.Cancelled
            : Telemetry.Success;
        Telemetry.Finish(_activity, outcome, _error);
    }
}
