using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

/// <summary>
/// Provides ATT and GATT helper methods for <see cref="AclConnection"/>.
/// </summary>
public static class AclConnectionExtensions
{
    /// <summary>
    /// Sends an ATT request over the connection and waits for the matching response.
    /// </summary>
    /// <typeparam name="TAttRequest">The ATT request type to send.</typeparam>
    /// <typeparam name="TResponse">The ATT response type expected in return.</typeparam>
    /// <param name="connection">The connection used to send the request.</param>
    /// <param name="request">The ATT request to transmit.</param>
    /// <param name="timeout">The maximum time to wait for the ATT response.</param>
    /// <param name="cancellationToken">Cancels the operation while waiting for the response.</param>
    /// <returns>The ATT response or ATT error returned by the peer.</returns>
    public static async Task<AttResponse<TResponse>> QueryAttPduAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(
        this AclConnection connection,
        TAttRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
        where TAttRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(connection);
        timeout ??= TimeSpan.FromMilliseconds(connection.Device.Settings.DefaultAttTimeoutMs);
        if (connection.DisconnectToken.IsCancellationRequested)
            throw connection.CreateDisconnectedException($"ATT query {request.OpCode}");
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            connection.DisconnectToken
        );
        using Activity? activity = Logging.StartHandleQueryAttPduActivity(request, connection);

        var responseSink = new AttResponseMessageSinkProvider<TResponse>(TAttRequest.ExpectedOpCode);
        using IDisposable _ = connection.Assembler.Subscribe(responseSink);
        try
        {
            connection.EnqueueGattPacket(request, activity);
            AttResponse<TResponse> response = await responseSink
                .Task.WaitAsync(timeout.Value, tokenSource.Token)
                .ConfigureAwait(false);
            if (response.IsSuccess)
                activity?.SetDeconstructedTags("Response", response.Value, orderEntries: true);
            else
                activity?.SetDeconstructedTags("Response", response.Error, orderEntries: true);
            string responseName = response.OpCode.ToString().ToUpperInvariant();
            activity?.SetTag("Response.OpCode", responseName);
            return response;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && connection.DisconnectToken.IsCancellationRequested)
        {
            throw connection.CreateDisconnectedException($"ATT query {request.OpCode}", exception);
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(e);
            throw;
        }
    }

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
        const ushort attCId = 0x0004;
        byte[] payloadBytes = attPdu.ToArrayLittleEndian();
        activity?.SetDeconstructedTags("Response", attPdu, orderEntries: true);

        using (connection.Logger?.BeginDeconstructedScope(LogLevel.Trace, "Packet", attPdu, orderEntries: true))
        using (connection.Logger?.ForContext("PacketPayload", payloadBytes))
        {
            connection.Logger?.LogAttPacketTransmission(
                "Host",
                "Controller",
                connection.ConnectionHandle,
                attPdu.OpCode.ToString().ToUpperInvariant()
            );
        }
        connection.AclPacketQueue.EnqueueL2CapBasic(connection.ConnectionHandle, attCId, payloadBytes);
    }

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

    private static void EnqueueL2CapBasic(
        this IAclPacketQueue packetQueue,
        ushort connectionHandle,
        ushort channelIdentifier,
        ReadOnlySpan<byte> payloadBytes
    )
    {
        int numberOfRemainingBytes = 4 + payloadBytes.Length;
        var offset = 0;
        var packetBoundaryFlag = PacketBoundaryFlag.FirstNonAutoFlushable;
        const BroadcastFlag broadcastFlag = BroadcastFlag.PointToPoint;
        while (numberOfRemainingBytes > 0)
        {
            ushort totalLength = Math.Min((ushort)numberOfRemainingBytes, packetQueue.MaxPacketSize);
            var l2CApBytes = new byte[totalLength];
            Span<byte> l2CApSpan = l2CApBytes;
            if (offset < 4)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(l2CApSpan, (ushort)payloadBytes.Length);
                BinaryPrimitives.WriteUInt16LittleEndian(l2CApSpan[2..], channelIdentifier);
                payloadBytes[..(totalLength - 4)].CopyTo(l2CApSpan[4..]);
            }
            else
            {
                payloadBytes[offset..(offset + totalLength)].CopyTo(l2CApSpan);
            }

            packetQueue.Enqueue(
                new HciAclPacket(connectionHandle, packetBoundaryFlag, broadcastFlag, totalLength, l2CApBytes)
            );
            packetBoundaryFlag = PacketBoundaryFlag.ContinuingFragment;
            offset += totalLength;
            numberOfRemainingBytes -= totalLength;
        }
    }
}
