using Microsoft.Extensions.Logging;

namespace Darp.Ble;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Trace, Message = "Ble device '{DeviceName}' initialized!")]
    public static partial void LogBleDeviceInitialized(this ILogger logger, string? deviceName);
    [LoggerMessage(Level = LogLevel.Trace, Message = "Ble device '{DeviceName}' disposed!")]
    public static partial void LogBleDeviceDisposed(this ILogger logger, string? deviceName);
}