using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> Records every log entry; optionally fails every write to test what happens when logging throws </summary>
internal sealed class RecordingLoggerFactory(bool throwOnLog = false) : ILoggerFactory, ILogger
{
    private readonly bool _throwOnLog = throwOnLog;

    public List<(string Category, LogLevel Level, EventId EventId, Exception? Exception)> Entries { get; } = [];

    private string _lastCategory = "";

    public ILogger CreateLogger(string categoryName)
    {
        _lastCategory = categoryName;
        return this;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (_throwOnLog)
            throw new InvalidOperationException("Logging failed");
        Entries.Add((_lastCategory, logLevel, eventId, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }
}
