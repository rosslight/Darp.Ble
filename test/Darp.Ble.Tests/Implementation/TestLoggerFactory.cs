using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble.Tests.Implementation;

public interface ITestLogger : ILogger
{
    string CategoryName { get; }

    IReadOnlyList<(LogLevel, string)> LogEntries { get; }
}

file sealed class TestLogger<T>(string categoryName) : ILogger<T>, ITestLogger
{
    private readonly List<(LogLevel, string)> _logEntries = [];

    public string CategoryName { get; } = categoryName;
    public IReadOnlyList<(LogLevel, string)> LogEntries => _logEntries.AsReadOnly();

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => throw new NotSupportedException();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        string message = formatter(state, exception);
        _logEntries.Add((logLevel, message));
    }
}

public sealed class TestLoggerFactory : ILoggerFactory
{
    private readonly Dictionary<string, ITestLogger> _loggers = [];

    public ITestLogger GetLogger(string categoryName) => _loggers[categoryName];

    public ITestLogger GetLogger<T>() => GetLogger(typeof(T).FullName ?? string.Empty);

    public ILogger CreateLogger(string categoryName)
    {
        if (_loggers.TryGetValue(categoryName, out var logger))
            return logger;

        Type? categoryType = Type.GetType(categoryName) ?? GetTypeFromLoadedAssemblies(categoryName);
        if (categoryType is null)
            return NullLogger.Instance;

        Type openGeneric = typeof(TestLogger<>);
        Type closedGeneric = openGeneric.MakeGenericType(categoryType);
        var testLogger = (ITestLogger?)Activator.CreateInstance(closedGeneric, categoryName);
        if (testLogger is null)
            return NullLogger.Instance;
        _loggers[categoryName] = testLogger;
        return testLogger;
    }

    private static Type? GetTypeFromLoadedAssemblies(string fullName)
    {
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false))
            .OfType<Type>()
            .FirstOrDefault();
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
