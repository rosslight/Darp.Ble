using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Trace, Message = "Enqueueing packet {@Packet}")]
    public static partial void LogEnqueuePacket(this ILogger logger, IHciPacket packet);
    [LoggerMessage(Level = LogLevel.Trace, Message = "Starting query of {@Command}")]
    public static partial void LogStartQuery(this ILogger logger, IHciPacket commandPacket);
}