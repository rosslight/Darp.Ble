using Microsoft.Extensions.Logging;

namespace Darp.Ble.Tests.Implementation;

public sealed class TestLogger : ILogger
{
    private readonly List<(LogLevel, string)> _logEntries = [];
    public IReadOnlyList<(LogLevel, string)> LogEntries => _logEntries.AsReadOnly();
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => throw new NotSupportedException();
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        _logEntries.Add((logLevel, message));
    }
}

public sealed class TestLoggerFactory(TestLogger logger) : ILoggerFactory
{
    private readonly TestLogger _logger = logger;

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // No-op: This factory doesn't use external providers.
    }

    public void Dispose()
    {
        // Nothing to dispose in this simple implementation.
    }
}