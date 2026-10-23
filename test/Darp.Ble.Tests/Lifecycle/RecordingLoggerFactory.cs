using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary>
/// Records every log entry; optionally fails every write to test what happens when logging throws, or runs a callback
/// for each entry, so a test can act at the moment the library logs something
/// </summary>
internal sealed class RecordingLoggerFactory(
    bool throwOnLog = false,
    Action<RecordingLoggerFactory.Entry>? onLog = null
) : ILoggerFactory
{
    private readonly bool _throwOnLog = throwOnLog;
    private readonly Action<Entry>? _onLog = onLog;

    public sealed record Entry(string Category, LogLevel Level, EventId EventId, Exception? Exception, string Message);

    private readonly List<Entry> _entries = [];

    /// <summary> What was logged so far; the library logs from several threads </summary>
    public IReadOnlyList<Entry> Entries
    {
        get
        {
            lock (_entries)
                return [.. _entries];
        }
    }

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, this);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class RecordingLogger(string category, RecordingLoggerFactory factory) : ILogger
    {
        private readonly string _category = category;
        private readonly RecordingLoggerFactory _factory = factory;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (_factory._throwOnLog)
                throw new InvalidOperationException("Logging failed");
            var entry = new Entry(_category, logLevel, eventId, exception, formatter(state, exception));
            lock (_factory._entries)
                _factory._entries.Add(entry);
            _factory._onLog?.Invoke(entry);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
    }
}
