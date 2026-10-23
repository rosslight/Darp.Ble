using Darp.Ble.Data;
using Darp.Ble.Gap;
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

    [LoggerMessage(
        EventId = 1100,
        EventName = "ScanStarted",
        Level = LogLevel.Debug,
        Message = "Radio scan started ({ScanType})"
    )]
    public static partial void LogScanStarted(this ILogger logger, ScanType scanType);

    [LoggerMessage(EventId = 1101, EventName = "ScanStopped", Level = LogLevel.Debug, Message = "Radio scan stopped")]
    public static partial void LogScanStopped(this ILogger logger);

    [LoggerMessage(
        EventId = 1102,
        EventName = "ScanReconfigured",
        Level = LogLevel.Warning,
        Message = "Scan settings changed while scanning, from {Previous} to {Current}; the radio scan restarts"
    )]
    public static partial void LogScanReconfigured(
        this ILogger logger,
        BleScanParameters previous,
        BleScanParameters current
    );

    [LoggerMessage(EventId = 1103, EventName = "ScanFailed", Level = LogLevel.Warning, Message = "Scan failed")]
    public static partial void LogScanFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1104,
        EventName = "ScanQueueOverflow",
        Level = LogLevel.Warning,
        Message = "A scan ended because its queue of {Capacity} advertisements was full"
    )]
    public static partial void LogScanQueueOverflow(this ILogger logger, int capacity);
}
