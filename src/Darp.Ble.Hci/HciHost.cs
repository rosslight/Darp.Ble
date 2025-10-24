using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    ulong ServerAddress { get; }
    ulong ClientAddress { get; }
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
public sealed partial class HciHost(ITransportLayer transportLayer, ulong randomAddress, ILogger<HciHost> logger)
    : IDisposable
{
    private readonly ITransportLayer _transportLayer = transportLayer;
    internal ILogger Logger { get; } = logger;
    private AclPacketQueue? _aclPacketQueue;
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary> The ACL Packet queue </summary>
    public IAclPacketQueue AclPacketQueue => _aclPacketQueue ?? throw new Exception("Not initialized yet");

    /// <summary> The random address </summary>
    public ulong Address { get; private set; } = randomAddress;

    private readonly ConcurrentDictionary<ushort, IAclConnection> _connections = [];
    private readonly SemaphoreSlim _packetInFlightSemaphore = new(1);

    /// <summary> Initialize the host </summary>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_isInitialized)
            throw new Exception("Already initialized");
        _isInitialized = true;
        await _transportLayer.InitializeAsync(OnReceivedPacket, cancellationToken).ConfigureAwait(false);
        Activity? activity = Logging.StartInitializeHciHostActivity();
        try
        {
            // Reset the controller
            await this.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(cancellationToken)
                .ConfigureAwait(false);
            // await Host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>();
            HciReadLocalVersionInformationResult version = await this.QueryCommandCompletionAsync<
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
            var data = await this.QueryCommandCompletionAsync<
                HciLeReadBufferSizeCommandV1,
                HciLeReadBufferSizeResultV1
            >(cancellationToken)
                .ConfigureAwait(false);
            _aclPacketQueue = new AclPacketQueue(
                this,
                _transportLayer,
                data.LeAclDataPacketLength,
                data.TotalNumLeAclDataPackets
            );
        }
        catch (Exception e)
        {
            activity?.AddException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
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
                        break;
                    case HciEventCode.HCI_Command_Status
                        when HciCommandStatusEvent.TryReadLittleEndian(x.DataBytes.Span, out var evt):
                        PublishMessage(evt);
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
                        Logger.LogWarning("Unknown Hci event {SubEventCode}", x.EventCode);
                        break;
                }
                break;
            case { PacketType: HciPacketType.HciAclData }
                when HciAclPacket.TryReadLittleEndian(packet.Pdu, out HciAclPacket x):
                PublishMessage(x);
                break;
            default:
                Logger.LogWarning("Unknown packet type {PacketType}", packet.PacketType);
                break;
        }
    }

    private void OnReceivedMetaEvent(HciLeMetaEvent metaEvt)
    {
        switch (metaEvt.SubEventCode)
        {
            case HciLeMetaSubEventType.HCI_LE_Connection_Update_Complete
                when HciLeConnectionUpdateCompleteEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                OnHciConnectionCompleteEvent(evt);
                break;
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

    private void OnHciConnectionCompleteEvent(HciLeConnectionUpdateCompleteEvent updateCompleteEvent)
    {
        Logger.LogTrace(
            "Connection parameters updated for connection {Handle} {Event}",
            updateCompleteEvent.ConnectionHandle,
            updateCompleteEvent
        );
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
    internal void EnqueuePacket(IHciPacket packet)
    {
        //Logger.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    /// <summary> Query a command expecting a <typeparamref name="TEvent"/> </summary>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response. If null, a timeout of 5000 is assumed </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TEvent"> The type of the event packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public async Task<TEvent> QueryCommandAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEvent
    >(TCommand command, TimeSpan? timeout, CancellationToken cancellationToken)
        where TCommand : IHciCommand
        where TEvent : IHciEvent<TEvent>
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        timeout ??= TimeSpan.FromSeconds(5);
        using var handler = new HciPacketInFlightHandler<TCommand, TEvent>(this, _packetInFlightSemaphore);
        (TEvent response, Activity? activity) = await handler
            .QueryAsync(command, timeout.Value, cancellationToken)
            .ConfigureAwait(false);
        activity?.Dispose();
        return response;
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TResponse"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public async Task<TResponse> QueryCommandCompletionAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(TCommand command, TimeSpan? timeout, CancellationToken cancellationToken)
        where TCommand : IHciCommand
        where TResponse : ICommandStatusResult, IBinaryReadable<TResponse>
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        timeout ??= TimeSpan.FromSeconds(5);
        using var handler = new HciPacketInFlightHandler<TCommand, HciCommandCompleteEvent>(
            this,
            _packetInFlightSemaphore
        );
        (HciCommandCompleteEvent response, Activity? activity) = await handler
            .QueryAsync(command, timeout.Value, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (response.CommandOpCode != TCommand.OpCode)
            {
                throw new HciException(
                    $"Command failed because response OpCode {response.CommandOpCode} is not {TCommand.OpCode}"
                );
            }

            if (!TResponse.TryReadLittleEndian(response.ReturnParameters.Span, out TResponse? parameters))
                throw new HciException("Command failed because response could not be read");

            activity?.SetDeconstructedTags("Response.Parameters", parameters, orderEntries: true, writeRawBytes: false);
            if (parameters.Status is not HciCommandStatus.Success)
                activity?.SetStatus(ActivityStatusCode.Error);
            return parameters;
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <summary> Writes a new, random address to the host </summary>
    /// <param name="randomAddress"></param>
    /// <param name="cancellationToken"></param>
    public async Task SetRandomAddressAsync(ulong randomAddress, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!_isInitialized)
        {
            Address = randomAddress;
            return;
        }
        await this.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(
                new HciLeSetRandomAddressCommand(randomAddress),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        Address = randomAddress;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _isDisposed = true;
        _transportLayer.Dispose();
        _aclPacketQueue?.Dispose();
        _packetInFlightSemaphore.Dispose();
    }
}

public static class L2CapHelpers
{
    public static async Task<AttResponse<TResponse>> QueryAttPduAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(
        this IAclConnection connection,
        TAttRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
        where TAttRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(connection);
        timeout ??= TimeSpan.FromSeconds(30);
        using Activity? activity = Logging.StartHandleQueryAttPduActivity(request, connection);

        var responseSink = new AttResponseMessageSinkProvider<TResponse>(TAttRequest.ExpectedOpCode);
        connection.L2CapAssembler.Subscribe(responseSink);
        try
        {
            connection.EnqueueGattPacket(request, activity, isResponse: false);
            AttResponse<TResponse> response = await responseSink
                .Task.WaitAsync(timeout.Value, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccess)
                activity?.SetDeconstructedTags("Response", response.Value, orderEntries: true);
            else
                activity?.SetDeconstructedTags("Response", response.Error, orderEntries: true);
            string responseName = response.OpCode.ToString().ToUpperInvariant();
            activity?.SetTag("Response.OpCode", responseName);
            connection.Host.Logger.LogTrace(
                "HciClient: Finished att request {@Request} with response {@Response}",
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
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(e);
            throw;
        }
    }

    /// <summary> Enqueue a new acl packet </summary>
    /// <param name="connection"> The connection to send this packet to </param>
    /// <param name="attPdu"> The packet to be enqueued </param>
    /// <param name="activity"></param>
    /// <param name="isResponse"> If true, the request will be logged as a response. If false, as a request </param>
    public static void EnqueueGattPacket<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(this IAclConnection connection, TAttPdu attPdu, Activity? activity, bool isResponse)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(connection);
        const ushort attCId = 0x0004;
        byte[] payloadBytes = attPdu.ToArrayLittleEndian();
        activity?.SetDeconstructedTags("Pdu", attPdu, orderEntries: true);

        using (connection.Logger.BeginDeconstructedScope(LogLevel.Trace, "Packet", attPdu, orderEntries: true))
        {
            connection.Logger.LogTrace(
                "Enqueued att {Direction} {OpCode} on channel {CId}",
                isResponse ? "response" : "request",
                attPdu.OpCode,
                attCId
            );
        }
        connection.AclPacketQueue.EnqueueL2CapBasic(connection.ConnectionHandle, attCId, payloadBytes);
    }

    public static void EnqueueGattErrorResponse(
        this IAclConnection connection,
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
        this IAclConnection connection,
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
            activity,
            isResponse: false
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

internal sealed partial class AttResponseMessageSinkProvider<TResponse>(AttOpCode expectedOpCode)
    where TResponse : IAttPdu
{
    private readonly AttOpCode _expectedOpCode = expectedOpCode;

    private readonly TaskCompletionSource<AttResponse<TResponse>> _tcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    [MessageSink]
    private void OnValue<T>(T value)
        where T : allows ref struct
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (typeof(T) == typeof(TResponse))
        {
            TResponse response = Unsafe.As<T, TResponse>(ref value);
            _tcs.TrySetResult(AttResponse<TResponse>.Ok(response));
        }
        else if (typeof(T) == typeof(AttErrorRsp))
        {
            AttErrorRsp errorResponse = Unsafe.As<T, AttErrorRsp>(ref value);
            if (_expectedOpCode != errorResponse.RequestOpCode)
                return;
            _tcs.TrySetResult(AttResponse<TResponse>.Fail(errorResponse));
        }
    }

    public Task<AttResponse<TResponse>> Task => _tcs.Task;
}
