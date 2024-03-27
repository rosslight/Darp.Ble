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
    [LoggerMessage(Level = LogLevel.Trace, Message = "H4Transport: {Direction} disconnected")]
    public static partial void LogTransportDisconnected(this ILogger logger, string direction);
    [LoggerMessage(Level = LogLevel.Critical, Message = "H4Transport: {Direction} died due to exception {Message}. This error is not recoverable!")]
    public static partial void LogTransportWithError(this ILogger logger, Exception ex, string direction, string message);
    [LoggerMessage(Level = LogLevel.Warning, Message = "H4Transport: Could not send packet {@Packet} due to error while encoding")]
    public static partial void LogPacketSendingErrorEncoding(this ILogger logger, IHciPacket packet);
    [LoggerMessage(Level = LogLevel.Trace, Message = "H4Transport: Sending packet {@Packet} with bytes 0x{@Bytes}")]
    public static partial void LogPacketSending(this ILogger logger, IHciPacket packet, byte[] bytes);
    [LoggerMessage(Level = LogLevel.Warning, Message = "H4Transport: Could not decode bytes 0x{PacketBytes:X2}{Bytes} to match packet {PacketType}")]
    public static partial void LogPacketReceivingDecodingFailed(this ILogger logger, byte packetBytes, byte[] bytes, string packetType);
    [LoggerMessage(Level = LogLevel.Trace, Message = "Read bytes 0x{PacketBytes:X2}{Bytes} of {PacketType} packet {@Packet}")]
    public static partial void LogPacketReceiving(this ILogger logger, byte packetBytes, byte[] bytes, HciPacketType packetType, IHciPacket packet);
    [LoggerMessage(Level = LogLevel.Warning, Message = "H4Transport: Received unknown hci packet of type 0x{Type:X2}. Reading remaining buffer ...")]
    public static partial void LogPacketReceivingUnknownPacket(this ILogger logger, byte type);
    [LoggerMessage(Level = LogLevel.Trace, Message = "HciHost: Query {@Command} from client completed successfully: Received {EventCode} {@Packet}")]
    public static partial void LogQueryCompleted(this ILogger logger, IEncodable command, HciEventCode eventCode, HciEventPacket packet);
    [LoggerMessage(Level = LogLevel.Error, Message = "HciHost: Query {@Command} from client failed because of {Reason}")]
    public static partial void LogQueryWithException(this ILogger logger, Exception ex, IEncodable command, string reason);
    [LoggerMessage(Level = LogLevel.Trace, Message = "HciHost: Query {@Command} from client started with status {Status}: Received {EventCode} {@Packet}")]
    public static partial void LogQueryStarted(this ILogger logger, IEncodable command, HciCommandStatus status, HciEventCode eventCode, HciEventPacket packet);
}