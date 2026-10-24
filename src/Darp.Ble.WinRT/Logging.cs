using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

internal static partial class Logging
{
    [LoggerMessage(
        EventId = 3000,
        EventName = "NotOnWindows",
        Level = LogLevel.Warning,
        Message = "The WinRT backend has no adapter: the app does not run on Windows 10 2004 or later, or was built without its Windows target"
    )]
    public static partial void LogNotOnWindows(this ILogger logger);

    [LoggerMessage(
        EventId = 3001,
        EventName = "ReservedSectionDropped",
        Level = LogLevel.Warning,
        Message = "Windows sets advertising data section {AdType} itself; the app's section is left out"
    )]
    public static partial void LogReservedSectionDropped(this ILogger logger, byte adType);

    [LoggerMessage(
        EventId = 3002,
        EventName = "HostedRequestFailed",
        Level = LogLevel.Warning,
        Message = "A central's request to a hosted service could not be answered"
    )]
    public static partial void LogHostedRequestFailed(this ILogger logger, Exception exception);
}
