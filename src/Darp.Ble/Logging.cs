using Microsoft.Extensions.Logging;

namespace Darp.Ble;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Trace, Message = "Ble device initialized!")]
    public static partial void LogBleDeviceInitialized(this ILogger logger);
}