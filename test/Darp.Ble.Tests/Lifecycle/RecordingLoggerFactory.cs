using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> Records every log entry; optionally fails every write to test what happens when logging throws </summary>
internal sealed class RecordingLoggerFactory(bool throwOnLog = false) : ILoggerFactory
{
    private readonly bool _throwOnLog = throwOnLog;

    public sealed record Entry(string Category, LogLevel Level, EventId EventId, Exception? Exception);

    public List<Entry> Entries { get; } = [];

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
            _factory.Entries.Add(new Entry(_category, logLevel, eventId, exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
    }
}
