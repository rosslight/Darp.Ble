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

public static class AclConnectionExtensions
{
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
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(e);
            throw;
        }
    }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="connection"> The connection to send this packet to </param>
    /// <param name="attPdu"> The packet to be enqueued </param>
    /// <param name="activity"></param>
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

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="connection"> The connection to send this packet to </param>
    /// <param name="attPdu"> The packet to be enqueued </param>
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
