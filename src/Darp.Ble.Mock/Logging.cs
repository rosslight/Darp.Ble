using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal static partial class Logging
{
    [LoggerMessage(
        EventId = 4000,
        EventName = "BackgroundWorkFailed",
        Level = LogLevel.Error,
        Message = "Background work of a simulated peripheral failed"
    )]
    public static partial void LogBackgroundWorkFailed(this ILogger logger, Exception exception);
}
