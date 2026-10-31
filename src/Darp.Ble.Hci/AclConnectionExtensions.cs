using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary>
/// Provides ATT and GATT helper methods for <see cref="AclConnection"/>.
/// </summary>
public static class AclConnectionExtensions
{
    /// <summary>
    /// Encodes an ATT PDU and queues it on the connection's ATT channel.
    /// </summary>
    /// <typeparam name="TAttPdu">The ATT PDU type to send.</typeparam>
    /// <param name="connection">The connection used to send the PDU.</param>
    /// <param name="attPdu">The ATT PDU to encode and enqueue.</param>
    /// <param name="activity">The activity to annotate with packet details.</param>
    public static void EnqueueGattPacket<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(this AclConnection connection, TAttPdu attPdu, Activity? activity)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        ArgumentNullException.ThrowIfNull(connection);
        activity?.SetDeconstructedTags("Response", attPdu, orderEntries: true);
        // Dropped once the link is down: nobody waits for it
        if (connection.DisconnectToken.IsCancellationRequested)
            return;
        (byte[] frame, int length) = Encode(connection, attPdu);
        connection.AclPacketQueue.Post(connection.ConnectionHandle, frame, length, IsAnswer(attPdu.OpCode));
    }

    /// <summary>
    /// Encodes an ATT PDU, sends it on the connection's ATT channel and waits until the controller has a buffer for it.
    /// From then on the PDU is sent completely while the link is up, whatever happens to the caller
    /// </summary>
    /// <typeparam name="TAttPdu">The ATT PDU type to send.</typeparam>
    /// <param name="connection">The connection used to send the PDU.</param>
    /// <param name="attPdu">The ATT PDU to encode and send; its bytes are copied before this returns.</param>
    /// <param name="onHandOff">Called when the PDU goes to the transport, before an answer to it can arrive. Must not block.</param>
    /// <param name="cancellationToken">Removes the PDU unsent while it waits for a buffer.</param>
    /// <param name="released">Removes the PDU unsent too, e.g. when the owner of the connection lets it go.</param>
    /// <returns>A task that completes once the PDU was handed to the transport; synchronously when a buffer is free.</returns>
    /// <exception cref="HciException">The link or the transport is down.</exception>
    public static ValueTask SendGattPacketAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(
        this AclConnection connection,
        TAttPdu attPdu,
        Action? onHandOff,
        CancellationToken cancellationToken,
        CancellationToken released = default
    )
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.DisconnectToken.IsCancellationRequested)
            return ValueTask.FromException(connection.CreateDisconnectedException($"send {attPdu.OpCode}"));
        (byte[] frame, int length) = Encode(connection, attPdu);
        return connection.AclPacketQueue.SendAsync(
            connection.ConnectionHandle,
            frame,
            length,
            IsAnswer(attPdu.OpCode),
            onHandOff,
            cancellationToken,
            released
        );
    }

    /// <summary> Writes the L2CAP basic frame of the PDU into an array of the shared pool </summary>
    private static (byte[] Frame, int Length) Encode<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(AclConnection connection, TAttPdu attPdu)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        const ushort attChannelId = 0x0004;
        int pduLength = attPdu.GetByteCount();
        int length = 4 + pduLength;
        byte[] frame = ArrayPool<byte>.Shared.Rent(length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)pduLength);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), attChannelId);
        if (!attPdu.TryWriteLittleEndian(frame.AsSpan(4, pduLength)))
        {
            ArrayPool<byte>.Shared.Return(frame);
            throw new HciException($"The {attPdu.OpCode} PDU could not be encoded");
        }
        if (connection.Logger?.IsEnabled(LogLevel.Trace) is true)
        {
            using (connection.Logger.BeginDeconstructedScope(LogLevel.Trace, "Packet", attPdu, orderEntries: true))
            using (connection.Logger.ForContext("PacketPayload", frame.AsSpan(4, pduLength).ToArray()))
            {
                connection.Logger.LogAttPacketTransmission(
                    "Host",
                    "Controller",
                    connection.ConnectionHandle,
                    attPdu.OpCode.ToString().ToUpperInvariant()
                );
            }
        }
        return (frame, length);
    }

    /// <summary> Answers of this host's ATT server to requests of the peer, which go before other traffic </summary>
    private static bool IsAnswer(AttOpCode opCode) =>
        opCode
            is AttOpCode.ATT_ERROR_RSP
                or AttOpCode.ATT_EXCHANGE_MTU_RSP
                or AttOpCode.ATT_FIND_INFORMATION_RSP
                or AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP
                or AttOpCode.ATT_READ_BY_TYPE_RSP
                or AttOpCode.ATT_READ_RSP
                or AttOpCode.ATT_READ_BLOB_RSP
                or AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP
                or AttOpCode.ATT_WRITE_RSP
                or AttOpCode.ATT_PREPARE_WRITE_RSP
                or AttOpCode.ATT_EXECUTE_WRITE_RSP;

    /// <summary>
    /// Queues an ATT error response for the specified request.
    /// </summary>
    /// <param name="connection">The connection used to send the response.</param>
    /// <param name="requestOpCode">The opcode of the request being rejected.</param>
    /// <param name="handle">The attribute handle associated with the error.</param>
    /// <param name="errorCode">The ATT error code to return.</param>
    /// <param name="activity">The activity to mark as failed.</param>
    public static void EnqueueGattErrorResponse(
        this AclConnection connection,
        AttOpCode requestOpCode,
        ushort handle,
        AttErrorCode errorCode,
        Activity? activity
    )
    {
        connection.EnqueueGattErrorResponse(exception: null, requestOpCode, handle, errorCode, activity);
    }

    /// <summary>
    /// Queues an ATT error response and records the associated exception details.
    /// </summary>
    /// <param name="connection">The connection used to send the response.</param>
    /// <param name="exception">The exception that caused the error response, if any.</param>
    /// <param name="requestOpCode">The opcode of the request being rejected.</param>
    /// <param name="handle">The attribute handle associated with the error.</param>
    /// <param name="errorCode">The ATT error code to return.</param>
    /// <param name="activity">The activity to mark as failed.</param>
    public static void EnqueueGattErrorResponse(
        this AclConnection connection,
        Exception? exception,
        AttOpCode requestOpCode,
        ushort handle,
        AttErrorCode errorCode,
        Activity? activity
    )
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        exception = exception is null
            ? new HciException($"Enqueued error response because of {errorCode}")
            : new HciException($"Enqueued error response because of {exception.Message}", exception);
        activity?.AddException(exception);
        connection.EnqueueGattPacket(
            new AttErrorRsp
            {
                RequestOpCode = requestOpCode,
                Handle = handle,
                ErrorCode = errorCode,
            },
            activity
        );
    }
}
