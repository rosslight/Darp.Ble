using Microsoft.Extensions.Logging;

namespace Darp.Ble;

internal static partial class Logging
{
    /// <summary> The logger category of the core </summary>
    public const string Category = "Darp.Ble";

    [LoggerMessage(
        EventId = 1000,
        EventName = "AdapterOpened",
        Level = LogLevel.Debug,
        Message = "Adapter {AdapterName} of backend {Backend} opened"
    )]
    public static partial void LogAdapterOpened(this ILogger logger, string backend, string? adapterName);

    [LoggerMessage(
        EventId = 1001,
        EventName = "AdapterDisposed",
        Level = LogLevel.Debug,
        Message = "Adapter {AdapterName} of backend {Backend} disposed"
    )]
    public static partial void LogAdapterDisposed(this ILogger logger, string backend, string? adapterName);

    [LoggerMessage(
        EventId = 1002,
        EventName = "AdapterOpenFailed",
        Level = LogLevel.Warning,
        Message = "Adapter {AdapterName} of backend {Backend} could not be opened: {Kind}"
    )]
    public static partial void LogAdapterOpenFailed(
        this ILogger logger,
        string backend,
        string? adapterName,
        BleOpenErrorKind kind,
        Exception exception
    );
}
