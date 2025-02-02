using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Ble device '{DeviceName}' initialized!")]
    public static partial void LogBleDeviceInitialized(this ILogger logger, string? deviceName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ble device '{DeviceName}' disposed!")]
    public static partial void LogBleDeviceDisposed(this ILogger logger, string? deviceName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Ble server peer '{Address}' connected!")]
    public static partial void LogBleServerPeerConnected(this ILogger logger, BleAddress address);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Ble server peer '{Address}' disposed!")]
    public static partial void LogBleServerPeerDisposed(this ILogger logger, BleAddress address);
}
