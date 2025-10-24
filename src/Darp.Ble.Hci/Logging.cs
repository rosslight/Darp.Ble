using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
internal static partial class Logging
{
    private static readonly ActivitySource HciActivity = new(HciLoggingStrings.ActivityName);
    private static readonly ActivitySource HciTracingActivity = new(HciLoggingStrings.TracingActivityName);

    public static Activity? StartInitializeHciHostActivity()
    {
        return HciActivity.StartActivity("Initialize HciHost");
    }

    public static Activity? StartCommandResponseActivity<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand
    >(TCommand command, ulong deviceAddress)
        where TCommand : IHciCommand
    {
        Activity? activity = HciActivity.StartActivity("Query command {Name}");
        if (activity is null)
            return activity;

        string commandName = TCommand.OpCode.ToString().ToUpperInvariant();
        activity.SetTag("Name", commandName);
        activity.SetTag("DeviceAddress", $"{deviceAddress:X12}");

        activity.SetDeconstructedTags("Request", command, orderEntries: true);
        activity.SetTag("Request.OpCode", $"{commandName}_COMMAND");
        return activity;
    }

    public static Activity? StartSendCommandActivity(HciOpCode commandOpCode, ulong deviceAddress)
    {
        Activity? activity = HciTracingActivity.StartActivity("Enqueue command {Name}");
        activity?.SetTag("Name", commandOpCode.ToString().ToUpperInvariant());
        activity?.SetTag("DeviceAddress", $"{deviceAddress:X12}");
        return activity;
    }

    public static Activity? StartHandleQueryAttPduActivity<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(TAttPdu request, IAclConnection aclConnection)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        Activity? activity = HciActivity.StartActivity("Query ATT {Name}");
        if (activity is null)
            return activity;

        string requestName = request.OpCode.ToString().ToUpperInvariant();
        activity.SetTag("Name", requestName);
        activity.SetTag("Connection.Handle", $"{aclConnection.ConnectionHandle:X4}");
        activity.SetTag("Connection.ServerAddress", aclConnection.ServerAddress);
        activity.SetTag("Connection.ClientAddress", aclConnection.ClientAddress);
        activity.SetTag("Connection.Role", "Server");

        activity.SetDeconstructedTags("Request", request, orderEntries: true);
        activity.SetTag("Request.OpCode", requestName);
        return activity;
    }

    public static Activity? StartWaitForEventActivity(HciEventCode eventCode, ulong deviceAddress)
    {
        Activity? activity = HciTracingActivity.StartActivity("Wait for event {Name}");
        activity?.SetTag("Name", eventCode.ToString().ToUpperInvariant());
        activity?.SetTag("DeviceAddress", $"{deviceAddress:X12}");
        return activity;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Enqueueing packet {@Packet}")]
    public static partial void LogEnqueuePacket(this ILogger logger, IHciPacket packet);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Starting query of {@Command}")]
    public static partial void LogStartQuery(this ILogger logger, IHciPacket command);

    [LoggerMessage(Level = LogLevel.Trace, Message = "H4Transport: {Direction} disconnected")]
    public static partial void LogTransportDisconnected(this ILogger logger, string direction);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "H4Transport: {Direction} died due to exception {Message}. This error is not recoverable!"
    )]
    public static partial void LogTransportWithError(
        this ILogger logger,
        Exception ex,
        string direction,
        string message
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "H4Transport: Could not send packet {@Packet} due to error while encoding"
    )]
    public static partial void LogPacketSendingErrorEncoding(this ILogger logger, IHciPacket packet);

    [LoggerMessage(Level = LogLevel.Trace, Message = "H4Transport: Sending packet {@Packet} with bytes 0x{@Bytes}")]
    public static partial void LogPacketSending(this ILogger logger, IHciPacket packet, byte[] bytes);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "H4Transport: Could not decode bytes 0x{PacketBytes:X2}{Bytes} to match packet {PacketType}"
    )]
    public static partial void LogPacketReceivingDecodingFailed(
        this ILogger logger,
        byte packetBytes,
        byte[] bytes,
        string packetType
    );

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Read bytes 0x{PacketBytes:X2}{Bytes} of {PacketType} packet {@Packet}"
    )]
    public static partial void LogPacketReceiving(
        this ILogger logger,
        byte packetBytes,
        byte[] bytes,
        HciPacketType packetType,
        IHciPacket packet
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "H4Transport: Received unknown hci packet of type 0x{Type:X2}. Reading remaining buffer ..."
    )]
    public static partial void LogPacketReceivingUnknownPacket(this ILogger logger, byte type);
}
