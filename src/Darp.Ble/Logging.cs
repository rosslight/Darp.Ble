using Darp.Ble.Connections;
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

    [LoggerMessage(
        EventId = 1200,
        EventName = "AdvertisingStarted",
        Level = LogLevel.Debug,
        Message = "Advertising set {Address} started"
    )]
    public static partial void LogAdvertisingStarted(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1201,
        EventName = "AdvertisingStopped",
        Level = LogLevel.Debug,
        Message = "Advertising set {Address} stopped"
    )]
    public static partial void LogAdvertisingStopped(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1300,
        EventName = "Connecting",
        Level = LogLevel.Debug,
        Message = "Connecting to {Address}"
    )]
    public static partial void LogConnecting(this ILogger logger, BleAddress address);

    [LoggerMessage(EventId = 1301, EventName = "Connected", Level = LogLevel.Debug, Message = "Connected to {Address}")]
    public static partial void LogConnected(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1302,
        EventName = "ConnectFailed",
        Level = LogLevel.Warning,
        Message = "Connecting to {Address} failed: {Kind}"
    )]
    public static partial void LogConnectFailed(
        this ILogger logger,
        BleAddress address,
        BleConnectErrorKind kind,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1303,
        EventName = "Disconnected",
        Level = LogLevel.Debug,
        Message = "Connection to {Address} ended: {Reason}"
    )]
    public static partial void LogDisconnected(this ILogger logger, BleAddress address, DisconnectReason reason);

    [LoggerMessage(
        EventId = 1307,
        EventName = "ConnectionCleanupFailed",
        Level = LogLevel.Warning,
        Message = "Releasing the connection to {Address} failed"
    )]
    public static partial void LogConnectionCleanupFailed(this ILogger logger, BleAddress address, Exception exception);

    [LoggerMessage(
        EventId = 1304,
        EventName = "RequestAbandoned",
        Level = LogLevel.Debug,
        Message = "A request to {Address} was abandoned after sending; the next request waits for its answer"
    )]
    public static partial void LogRequestAbandoned(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1305,
        EventName = "AbandonedRequestCompleted",
        Level = LogLevel.Debug,
        Message = "An abandoned request to {Address} ended after {Duration}"
    )]
    public static partial void LogAbandonedRequestCompleted(this ILogger logger, BleAddress address, TimeSpan duration);

    [LoggerMessage(
        EventId = 1306,
        EventName = "AttTimeout",
        Level = LogLevel.Warning,
        Message = "{Address} did not answer within the ATT timeout; ending the connection"
    )]
    public static partial void LogAttTimeout(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1400,
        EventName = "CentralConnected",
        Level = LogLevel.Debug,
        Message = "Central {Address} connected"
    )]
    public static partial void LogCentralConnected(this ILogger logger, BleAddress address);

    [LoggerMessage(
        EventId = 1401,
        EventName = "CentralDisconnected",
        Level = LogLevel.Debug,
        Message = "Connection of central {Address} ended: {Reason}"
    )]
    public static partial void LogCentralDisconnected(this ILogger logger, BleAddress address, DisconnectReason reason);
}
