using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Darp.Ble.Diagnostics;

namespace Darp.Ble.Tests.Diagnostics;

/// <summary> Records every measurement of the library's meter while it lives </summary>
internal sealed class RecordedMeasurements : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<(
        string Instrument,
        double Value,
        Dictionary<string, object?> Tags
    )> _measurements = new();

    public RecordedMeasurements()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (string.Equals(instrument.Meter.Name, BleTelemetry.MeterName, StringComparison.Ordinal))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.Start();
    }

    /// <summary> The measurements of one instrument whose tags contain all of the given ones </summary>
    public IReadOnlyList<double> Of(string instrument, params (string Key, object Value)[] tags) =>
        [
            .. _measurements
                .Where(m =>
                    string.Equals(m.Instrument, instrument, StringComparison.Ordinal)
                    && tags.All(tag => Equals(m.Tags.GetValueOrDefault(tag.Key), tag.Value))
                )
                .Select(m => m.Value),
        ];

    public void Dispose() => _listener.Dispose();

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> tag in tags)
            copy[tag.Key] = tag.Value;
        _measurements.Enqueue((instrument.Name, value, copy));
    }
}
