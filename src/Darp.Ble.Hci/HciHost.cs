using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public interface IAclConnection : IMessageSinkProvider
{
    HciHost Host { get; }
    ushort ConnectionHandle { get; }
    ushort AttMtu { get; }
    IAclPacketQueue AclPacketQueue { get; }
    IL2CapAssembler L2CapAssembler { get; }
    protected internal ILogger Logger { get; }
}

public readonly ref struct L2CapPdu(ushort channelId, ReadOnlySpan<byte> pdu)
{
    public ushort ChannelId { get; } = channelId;
    public ReadOnlySpan<byte> Pdu { get; } = pdu;
}

[BinaryObject]
public readonly partial struct HciPacketEvent
{
    public required HciEventCode EventCode { get; init; }
    public required byte ParameterTotalLength { get; init; }

    [BinaryLength(nameof(ParameterTotalLength))]
    public required ReadOnlyMemory<byte> DataBytes { get; init; }
}

/// <summary> The HCI Host </summary>
/// <param name="transportLayer"> The transport layer </param>
/// <param name="logger"> An optional logger </param>
[MessageSource]
public sealed partial class HciHost(ITransportLayer transportLayer, ILogger<HciHost> logger) : IDisposable
{
    private readonly ITransportLayer _transportLayer = transportLayer;
    internal ILogger Logger { get; } = logger;
    private AclPacketQueue? _aclPacketQueue;
    public IAclPacketQueue AclPacketQueue => _aclPacketQueue ?? throw new Exception("Not initialized yet");
    private readonly ConcurrentDictionary<ushort, IAclConnection> _connections = [];
    private readonly SemaphoreSlim _packetInFlightSemaphore = new(1);

    /// <summary> Initialize the host </summary>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _transportLayer.Initialize(OnReceivedPacket);
        // Reset the controller
        await this.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(cancellationToken)
            .ConfigureAwait(false);
        // await Host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>();
        var version = await this.QueryCommandCompletionAsync<
            HciReadLocalVersionInformationCommand,
            HciReadLocalVersionInformationResult
        >(cancellationToken)
            .ConfigureAwait(false);
        if (version.HciVersion < CoreVersion.BluetoothCoreSpecification42)
        {
            throw new NotSupportedException(
                $"Controller version {version.HciVersion} is not supported. Minimum required version is 4.2"
            );
        }
        await this.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(
                new HciSetEventMaskCommand((EventMask)0x3fffffffffffffff),
                cancellationToken
            )
            .ConfigureAwait(false);
        await this.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(
                new HciLeSetEventMaskCommand((LeEventMask)0xf0ffff),
                cancellationToken
            )
            .ConfigureAwait(false);
        var data = await this.QueryCommandCompletionAsync<HciLeReadBufferSizeCommandV1, HciLeReadBufferSizeResultV1>(
                cancellationToken
            )
            .ConfigureAwait(false);
        _aclPacketQueue = new AclPacketQueue(
            this,
            _transportLayer,
            data.LeAclDataPacketLength,
            data.TotalNumLeAclDataPackets
        );
    }

    private void OnReceivedPacket(HciPacket packet)
    {
        switch (packet)
        {
            case { PacketType: HciPacketType.HciEvent }
                when HciPacketEvent.TryReadLittleEndian(packet.Pdu, out HciPacketEvent x):
                switch (x.EventCode)
                {
                    case HciEventCode.HCI_Disconnection_Complete
                        when HciDisconnectionCompleteEvent.TryReadLittleEndian(x.DataBytes.Span, out var evt):
                        PublishMessage(evt);
                        if (_connections.TryGetValue(evt.ConnectionHandle, out IAclConnection? connection))
                        {
                            if (connection is IDisposable disposable)
                                disposable.Dispose();
                            _connections.TryRemove(evt.ConnectionHandle, out _);
                        }
                        break;
                    case HciEventCode.HCI_Command_Complete
                        when HciCommandCompleteEvent.TryReadLittleEndian(x.DataBytes.Span, out var evt):
                        PublishMessage(evt);
                        _packetInFlightSemaphore.Release();
                        break;
                    case HciEventCode.HCI_Command_Status
                        when HciCommandStatusEvent.TryReadLittleEndian(x.DataBytes.Span, out var evt):
                        PublishMessage(evt);
                        _packetInFlightSemaphore.Release();
                        break;
                    case HciEventCode.HCI_Number_Of_Completed_Packets
                        when HciNumberOfCompletedPacketsEvent.TryReadLittleEndian(x.DataBytes.Span, out var evt):
                        PublishMessage(evt);
                        break;
                    case HciEventCode.HCI_LE_Meta
                        when HciLeMetaEvent.TryReadLittleEndian(x.DataBytes.Span, out var leMetaEvent):
                        OnReceivedMetaEvent(leMetaEvent);
                        break;
                    case HciEventCode.None:
                    default:
                        Logger.LogTrace("Unknown Hci event {SubEventCode}", x.EventCode);
                        break;
                }
                break;
            case { PacketType: HciPacketType.HciAclData }
                when HciAclPacket.TryReadLittleEndian(packet.Pdu, out HciAclPacket x):
                PublishMessage(x);
                break;
            default:
                Logger.LogTrace("Unknown packet type {PacketType}", packet.PacketType);
                break;
        }
    }

    private void OnReceivedMetaEvent(HciLeMetaEvent metaEvt)
    {
        switch (metaEvt.SubEventCode)
        {
            case HciLeMetaSubEventType.HCI_LE_Data_Length_Change
                when HciLeDataLengthChangeEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1
            or HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_v2
                when HciLeEnhancedConnectionCompleteV1Event.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_PHY_Update_Complete
                when HciLePhyUpdateCompleteEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report
                when HciLeExtendedAdvertisingReportEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                break;
            default:
                Logger.LogTrace("Unknown LE Meta event {SubEventCode}", metaEvt.SubEventCode);
                break;
        }
    }

    /// <summary> Register a connection </summary>
    /// <param name="connection"> The connection to register </param>
    public void RegisterConnection(IAclConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        bool isAdded = _connections.TryAdd(connection.ConnectionHandle, connection);
        Debug.Assert(isAdded, $"Connection {connection.ConnectionHandle} could not be registered at host!");
    }

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    private void EnqueuePacket(IHciPacket packet)
    {
        Logger.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    /// <summary> Query a command expecting a <typeparamref name="TEvent"/> </summary>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="predicate"> Check whether the event is acceptable </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TEvent"> The type of the event packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public async Task<TEvent> QueryCommandAsync<TCommand, TEvent>(
        TCommand command,
        TimeSpan? timeout = null,
        Func<TEvent, bool>? predicate = null,
        CancellationToken cancellationToken = default
    )
        where TCommand : IHciCommand
        where TEvent : IHciEvent<TEvent>
    {
        timeout ??= TimeSpan.FromSeconds(10);
        await _packetInFlightSemaphore.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false);
        IObservable<TEvent> statusFailedObservable = Observable.Create<TEvent>(observer =>
            this.AsObservable<HciCommandStatusEvent>()
                .Subscribe(
                    statusEvent =>
                    {
                        if (
                            statusEvent.CommandOpCode != TCommand.OpCode
                            || statusEvent.Status is HciCommandStatus.Success
                        )
                        {
                            return;
                        }
                        observer.OnError(new HciException($"Command failed with status {statusEvent.Status}"));
                    },
                    observer.OnError,
                    observer.OnCompleted
                )
        );
        IObservable<TEvent> resultObservable = Observable.Create<TEvent>(observer =>
            this.AsObservable<TEvent>()
                .Subscribe(
                    completeEvent =>
                    {
                        if (predicate is not null && !predicate(completeEvent))
                            return;
                        observer.OnNext(completeEvent);
                    },
                    observer.OnError,
                    observer.OnCompleted
                )
        );
        var packet = new HciCommandPacket<TCommand>(command);
        Logger.LogStartQuery(packet);
        Task<TEvent> task = resultObservable
            .Concat(statusFailedObservable)
            .Timeout(timeout.Value)
            .FirstAsync()
            .Do(
                completePacket => Logger.LogQueryCompleted(command, TEvent.EventCode, completePacket),
                exception => Logger.LogQueryWithException(exception, command, exception.Message)
            )
            .ToTask(cancellationToken);
        EnqueuePacket(packet);
        return await task.ConfigureAwait(false);
    }

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public async Task<HciCommandStatus> QueryCommandStatusAsync<TCommand>(
        TCommand command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
        where TCommand : IHciCommand
    {
        timeout ??= TimeSpan.FromSeconds(10);
        await _packetInFlightSemaphore.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false);
        Task<HciCommandStatusEvent> statusTask = this.AsObservable<HciCommandStatusEvent>()
            .Where(static x => x.CommandOpCode == TCommand.OpCode)
            .FirstAsync()
            .Timeout(timeout.Value)
            .ToTask(cancellationToken);
        var packet = new HciCommandPacket<TCommand>(command);
        Logger.LogStartQuery(packet);
        EnqueuePacket(packet);
        HciCommandStatusEvent statusEvent = await statusTask.ConfigureAwait(false);
        return statusEvent.Status;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _transportLayer.Dispose();
        _aclPacketQueue?.Dispose();
        _packetInFlightSemaphore.Dispose();
    }
}

public static class L2CapHelpers
{
    public static async Task<AttResponse<TResponse>> QueryAttPduAsync<TAttRequest, TResponse>(
        this IAclConnection connection,
        TAttRequest request,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default
    )
        where TAttRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(connection);
        IObservable<AttResponse<TResponse>> valueObservable = connection
            .L2CapAssembler.AsObservable<TResponse>()
            .Select(AttResponse<TResponse>.Ok);
        IObservable<AttResponse<TResponse>> errorObservable = connection
            .L2CapAssembler.AsObservable<AttErrorRsp>()
            .Where(x => x.RequestOpCode == TAttRequest.ExpectedOpCode)
            .Select(AttResponse<TResponse>.Fail);
        IObservable<AttResponse<TResponse>> observable = valueObservable.Concat(errorObservable);
        if (timeout.TotalNanoseconds > 0)
            observable = observable.Timeout(timeout);
        try
        {
            AttResponse<TResponse> response = await observable
                .FirstAsync()
                .ToTask(cancellationToken)
                .ConfigureAwait(false);
            connection.Host.Logger.LogTrace(
                "HciClient: Finished att request {@Request} with result {@Result}",
                request,
                response
            );
            return response;
        }
        catch (Exception e)
        {
            connection.Host.Logger.LogWarning(
                e,
                "HciClient: Could not finish att request {@Request} because of {Message}",
                request,
                e.Message
            );
            throw;
        }
    }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="connection"> The connection to send this packet to </param>
    /// <param name="attPdu"> The packet to be enqueued </param>
    public static void EnqueueGattPacket<TAttPdu>(this IAclConnection connection, TAttPdu attPdu)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connection);
        const ushort attCId = 0x0004;
        byte[] payloadBytes = attPdu.ToArrayLittleEndian();
        connection.Logger.LogTrace("Enqueued att response {@Packet} on channel {CId}", attPdu, attCId);
        connection.AclPacketQueue.EnqueueL2CapBasic(connection.ConnectionHandle, attCId, payloadBytes);
    }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="connection"> The connection to send this packet to </param>
    /// <param name="attPdu"> The packet to be enqueued </param>
    public static void EnqueueGattErrorResponse(
        this IAclConnection connection,
        AttOpCode requestOpCode,
        ushort handle,
        AttErrorCode errorCode
    )
    {
        connection.EnqueueGattPacket(
            new AttErrorRsp
            {
                RequestOpCode = requestOpCode,
                Handle = handle,
                ErrorCode = errorCode,
            }
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
