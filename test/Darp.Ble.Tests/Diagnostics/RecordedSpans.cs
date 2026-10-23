using System.Collections.Concurrent;
using System.Diagnostics;
using Darp.Ble.Diagnostics;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> Records every finished span of the library while it lives </summary>
internal sealed class RecordedSpans : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentQueue<Activity> _spans = new();

    public RecordedSpans()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, BleTelemetry.SourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _spans.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public IReadOnlyList<Activity> All => [.. _spans];

    public Activity Single(string displayName) =>
        All.Single(span => string.Equals(span.DisplayName, displayName, StringComparison.Ordinal));

    public void Dispose() => _listener.Dispose();
}
