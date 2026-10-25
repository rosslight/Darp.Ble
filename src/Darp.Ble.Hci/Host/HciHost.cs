using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci.Host;

/// <summary> Provides the fatal exception raised by the transport layer. </summary>
public sealed class HciTransportFailedEventArgs(Exception exception) : EventArgs
{
    /// <summary> The fatal transport exception. </summary>
    public Exception Exception { get; } = exception;
}

/// <summary>
/// The <see cref="HciHost"/> is responsible for all host-related commands.
/// </summary>
/// <param name="hciDevice">The owning HCI device.</param>
/// <param name="transportLayer">The transport used to exchange packets with the controller.</param>
/// <param name="logger">An optional logger.</param>
public sealed partial class HciHost(HciDevice hciDevice, ITransportLayer transportLayer, ILogger<HciHost>? logger)
    : IMessageSource,
        IDisposable
{
    private readonly MessageSinks _sinks = new();
    private readonly ITransportLayer _transportLayer = transportLayer;
    private readonly ILogger<HciHost>? _logger = logger;

    // Commands go one at a time
    private readonly SemaphoreSlim _packetInFlightSemaphore = new(1);

    // Guards the fields below: the transport's thread reports the controller's room, the sending command takes it
    private readonly Lock _roomLock = new();

    // Completed while the controller reported room for a command since the last one was sent (Core Vol 4 Part E 4.4).
    // Every Command Complete and Command Status reports it, also the ones for no command (opcode 0x0000). Commands go
    // one at a time, so one report of room is enough; a command that is never answered keeps the next one waiting
    // for the controller's next report, which also keeps its late answer from answering the next command.
    // After a reset there is room for one
    private TaskCompletionSource _room = CompletedSource();

    // Counts the reports, so a command the transport refused gives its room back only if no report came since
    private int _roomReports;

    // Cancelled when the transport failed, and why; set once on the transport's thread, read by every command
    private readonly CancellationTokenSource _transportGone = new();
    private Exception? _transportFailure;

    // Set once by ResetAsync: the queue needs the controller's buffer sizes, and packets before the reset are ignored
    private AclPacketQueue? _leAclPacketQueue;
    private bool _isResetDoneAtLeastOnce;

    /// <summary>Raised when the transport encounters a fatal failure.</summary>
    public event EventHandler<HciTransportFailedEventArgs>? TransportFailed;

    /// <summary>Gets the owning HCI device.</summary>
    public HciDevice Device { get; } = hciDevice;

    /// <summary>Gets the ACL packet queue created during initialization.</summary>
    internal AclPacketQueue AclPacketQueue =>
        _leAclPacketQueue ?? throw new InvalidOperationException("Not initialized yet");

    /// <summary>Resets the controller and applies the required host configuration.</summary>
    /// <param name="token">Cancels initialization while waiting for controller responses.</param>
    /// <returns>A task that completes when the host is ready for use.</returns>
    public async Task ResetAsync(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Device.IsDisposed, this);
        await _transportLayer.InitializeAsync(OnReceivedPacket, OnTransportFailed, token).ConfigureAwait(false);
        Activity? activity = Logging.StartInitializeHciHostActivity();
        try
        {
            // Reset the controller
            await this.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(token).ConfigureAwait(false);
            _isResetDoneAtLeastOnce = true;

            // Read information about the controller
            await this.QueryCommandCompletionAsync<
                HciReadLocalSupportedCommandsCommand,
                HciReadLocalSupportedCommandsResult
            >(token)
                .ConfigureAwait(false);
            await this.QueryCommandCompletionAsync<
                HciLeReadLocalSupportedFeaturesCommand,
                HciLeReadLocalSupportedFeaturesResult
            >(token)
                .ConfigureAwait(false);
            HciReadLocalVersionInformationResult version = await this.QueryCommandCompletionAsync<
                HciReadLocalVersionInformationCommand,
                HciReadLocalVersionInformationResult
            >(token)
                .ConfigureAwait(false);
            if (version.HciVersion < CoreVersion.BluetoothCoreSpecification42)
            {
                throw new NotSupportedException(
                    $"Controller version {version.HciVersion} is not supported. Minimum required version is 4.2"
                );
            }

            await this.QueryCommandCompletionAsync<
                HciReadLocalSupportedFeaturesCommand,
                HciReadLocalSupportedFeaturesResult
            >(token)
                .ConfigureAwait(false);

            await this.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(
                    new HciSetEventMaskCommand((EventMask)0x3dbff807bffb9fff),
                    token
                )
                .ConfigureAwait(false);
            await this.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(
                    new HciLeSetEventMaskCommand((LeEventMask)0x00000007fff7ffff),
                    token
                )
                .ConfigureAwait(false);
            var data = await this.QueryCommandCompletionAsync<
                HciLeReadBufferSizeCommandV1,
                HciLeReadBufferSizeResultV1
            >(token)
                .ConfigureAwait(false);
            await this.QueryCommandCompletionAsync<
                HciLeReadSuggestedDefaultDataLengthCommand,
                HciLeReadSuggestedDefaultDataLengthResult
            >(token)
                .ConfigureAwait(false);
            await this.QueryCommandCompletionAsync<
                HciLeWriteSuggestedDefaultDataLengthCommand,
                HciLeWriteSuggestedDefaultDataLengthResult
            >(
                    new HciLeWriteSuggestedDefaultDataLengthCommand
                    {
                        SuggestedMaxTxOctets = 251,
                        SuggestedMaxTxTime = 2120,
                    },
                    token
                )
                .ConfigureAwait(false);
            _leAclPacketQueue = new AclPacketQueue(
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
        ObjectDisposedException.ThrowIf(Device.IsDisposed, this);
        timeout ??= Device.Settings.CommandTimeout;
        using var handler = new HciPacketInFlightHandler<TCommand, TEvent>(this);
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
        ObjectDisposedException.ThrowIf(Device.IsDisposed, this);
        timeout ??= Device.Settings.CommandTimeout;
        using var handler = new HciPacketInFlightHandler<TCommand, HciCommandCompleteEvent>(this);
        (HciCommandCompleteEvent response, Activity? activity) = await handler
            .QueryAsync(command, timeout.Value, cancellationToken)
            .ConfigureAwait(false);
        try
        {
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

    /// <summary> Whether a command could be sent right away </summary>
    internal bool IsCommandSlotFree
    {
        get
        {
            lock (_roomLock)
                return _packetInFlightSemaphore.CurrentCount > 0 && _room.Task.IsCompleted;
        }
    }

    /// <summary> Waits until no command is in flight and the controller has room for one, and takes the room </summary>
    /// <remarks> The caller sends its command right away and gives the slot back with <see cref="ReleaseCommandSlot"/> </remarks>
    /// <returns> The report the room came from, which <see cref="EnqueuePacket{T}"/> needs to give it back </returns>
    internal async Task<int> WaitForCommandSlotAsync(CancellationToken cancellationToken)
    {
        await _packetInFlightSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (true)
            {
                Task room;
                lock (_roomLock)
                {
                    if (_room.Task.IsCompleted)
                    {
                        _room = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        return _roomReports;
                    }
                    room = _room.Task;
                }
                await room.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            _packetInFlightSemaphore.Release();
            throw;
        }
    }

    /// <summary> Gives the slot taken with <see cref="WaitForCommandSlotAsync"/> back </summary>
    internal void ReleaseCommandSlot() => _packetInFlightSemaphore.Release();

    private void OnNumHciCommandPackets(byte numHciCommandPackets)
    {
        lock (_roomLock)
        {
            _roomReports++;
            if (numHciCommandPackets > 0)
                _room.TrySetResult();
            else if (_room.Task.IsCompleted)
                _room = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    /// <summary> Sends a command; the caller holds the slot and the room <see cref="WaitForCommandSlotAsync"/> took </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    /// <param name="roomReport"> The report the room came from </param>
    internal void EnqueuePacket<T>(HciCommandPacket<T> packet, int roomReport)
        where T : IHciCommand
    {
        using (_logger?.ForContext("PacketPayload", packet.ToArrayLittleEndian()))
        using (_logger?.ForContext("@Packet", packet.Data))
        {
            _logger?.LogPacketTransmission("Host", "Controller", $"{T.OpCode.ToString().ToUpperInvariant()}_COMMAND");
        }
        try
        {
            _transportLayer.Enqueue(packet);
        }
        catch
        {
            // The command never reached the controller, so the room is still there, unless the controller reported since
            lock (_roomLock)
            {
                if (_roomReports == roomReport)
                    _room.TrySetResult();
            }
            throw;
        }
    }

    private void OnReceivedPacket(HciPacket packet)
    {
        switch (packet)
        {
            case { PacketType: HciPacketType.HciEvent }
                when HciPacketEvent.TryReadLittleEndian(packet.Pdu, out HciPacketEvent x):
                OnReceivedHciEventPacket(x);
                break;
            case { PacketType: HciPacketType.HciAclData }
                when HciAclPacket.TryReadLittleEndian(packet.Pdu, out HciAclPacket x):
                if (!_isResetDoneAtLeastOnce)
                {
                    using (_logger?.ForContext("PacketPayload", x.DataBytes.ToArray()))
                    {
                        _logger?.LogAttPacketTransmissionWarning(
                            "Controller",
                            "Host",
                            "UNKNOWN",
                            x.ConnectionHandle,
                            "ignored because the host was not reset yet!"
                        );
                    }
                    break;
                }
                if (!Device.TryGetConnection(x.ConnectionHandle, out AclConnection? connection))
                {
                    using (_logger?.ForContext("PacketPayload", packet.Pdu.ToArray()))
                    {
                        _logger?.LogWarning(
                            "Received acl packet for {ConnectionHandle} without an associated connection",
                            x.ConnectionHandle
                        );
                    }
                    break;
                }
                connection.Assembler.OnAclPacket(x);
                break;
            default:
                using (_logger?.ForContext("PacketPayload", packet.Pdu.ToArray()))
                {
                    _logger?.LogWarning("Unknown packet type {PacketType}", packet.PacketType);
                }
                break;
        }
    }

    /// <summary> Cancelled when the transport failed; nothing the controller would answer arrives any more </summary>
    internal CancellationToken TransportGone => _transportGone.Token;

    /// <summary> Why the transport failed, once it did </summary>
    internal Exception? TransportFailure => Volatile.Read(ref _transportFailure);

    private void OnTransportFailed(Exception exception)
    {
        Volatile.Write(ref _transportFailure, exception);
        // The owners of the links learn about it before anything waiting on the transport fails
        TransportFailed?.Invoke(this, new HciTransportFailedEventArgs(exception));
        _leAclPacketQueue?.Close(exception);
        _transportGone.Cancel();
    }

    private void OnReceivedHciEventPacket(HciPacketEvent packet)
    {
        if (
            !_isResetDoneAtLeastOnce
            && (
                packet.EventCode is not HciEventCode.HCI_Command_Complete
                || HciCommandCompleteEvent.TryReadLittleEndian(packet.DataBytes.Span, out var completeEvent)
                    && completeEvent.CommandOpCode is not HciOpCode.HCI_Reset
            )
        )
        {
            using (_logger?.ForContext("PacketPayload", packet.DataBytes.ToArray()))
            {
                _logger?.LogPacketTransmissionWarning(
                    "Controller",
                    "Host",
                    $"{packet.EventCode.ToString().ToUpperInvariant()}_EVENT",
                    "ignored because the host was not reset yet!"
                );
            }
            return;
        }
        switch (packet.EventCode)
        {
            case HciEventCode.HCI_Command_Complete
                when HciCommandCompleteEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                // The answer goes to the command in flight first, so a command that waits for room cannot take it
                PublishMessage(evt);
                OnNumHciCommandPackets(evt.NumHciCommandPackets);
                break;
            case HciEventCode.HCI_Disconnection_Complete
                when HciDisconnectionCompleteEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                // The owners of the connection learn why it ended before anything waiting on it fails
                PublishMessage(evt);
                if (Device.TryGetConnection(evt.ConnectionHandle, out AclConnection? connection))
                    connection.OnDisconnectEvent(evt);
                Device.RemoveConnection(evt.ConnectionHandle, evt.Reason);
                break;
            case HciEventCode.HCI_Command_Status
                when HciCommandStatusEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                // The answer goes to the command in flight first, so a command that waits for room cannot take it
                PublishMessage(evt);
                OnNumHciCommandPackets(evt.NumHciCommandPackets);
                break;
            case HciEventCode.HCI_Number_Of_Completed_Packets
                when HciNumberOfCompletedPacketsEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                Debug.Assert(_leAclPacketQueue is not null);
#pragma warning disable CS0618 // This is the only place where its fine to enqueue this event
                _leAclPacketQueue?.OnHciNumberOfPacketsEvent(evt);
#pragma warning restore CS0618 // Type or member is obsolete
                PublishMessage(evt);
                break;
            case HciEventCode.HCI_LE_Meta
                when HciLeMetaEvent.TryReadLittleEndian(packet.DataBytes.Span, out var leMetaEvent):
                OnReceivedMetaEvent(leMetaEvent, packet.DataBytes);
                break;
            case HciEventCode.None:
            default:
                LogPacketControllerToHost(
                    packet,
                    packet.DataBytes,
                    $"{packet.EventCode.ToString().ToUpperInvariant()}_EVENT"
                );
                break;
        }
    }

    private void OnReceivedMetaEvent(HciLeMetaEvent metaEvt, ReadOnlyMemory<byte> packetPayloadBytes)
    {
        switch (metaEvt.SubEventCode)
        {
            case HciLeMetaSubEventType.HCI_LE_Connection_Update_Complete
                when HciLeConnectionUpdateCompleteEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                LogEventPacketControllerToHost(evt, packetPayloadBytes);
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Data_Length_Change
                when HciLeDataLengthChangeEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                LogEventPacketControllerToHost(evt, packetPayloadBytes);
                if (Device.TryGetConnection(evt.ConnectionHandle, out var conn))
                    conn.GattServer.OnHciLeDataLengthChangeEvent(evt);
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1
            or HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_v2
                when HciLeEnhancedConnectionCompleteV1Event.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                LogEventPacketControllerToHost(evt, packetPayloadBytes);
                Device.OnHciLeEnhancedConnectionCompleteV1EventPacket(evt);
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_PHY_Update_Complete
                when HciLePhyUpdateCompleteEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                LogEventPacketControllerToHost(evt, packetPayloadBytes);
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report
                when HciLeExtendedAdvertisingReportEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                PublishMessage(evt);
                break;
            case HciLeMetaSubEventType.HCI_LE_Advertising_Set_Terminated
                when HciLeAdvertisingSetTerminatedEvent.TryReadLittleEndian(metaEvt.Parameters.Span, out var evt):
                LogEventPacketControllerToHost(evt, packetPayloadBytes);
                PublishMessage(evt);
                break;
            default:
                LogPacketControllerToHost(
                    metaEvt,
                    packetPayloadBytes,
                    $"{metaEvt.SubEventCode.ToString().ToUpperInvariant()}_EVENT"
                );
                break;
        }
    }

    private void LogEventPacketControllerToHost<T>(T packet, ReadOnlyMemory<byte> packetPayloadBytes)
        where T : IHciEvent<T>
    {
        string packetName = packet is IHciLeMetaEvent<T> p
            ? $"{p.SubEventCode.ToString().ToUpperInvariant()}_EVENT"
            : $"{T.EventCode.ToString().ToUpperInvariant()}_EVENT";
        LogPacketControllerToHost(packet, packetPayloadBytes, packetName);
    }

    private void LogPacketControllerToHost<T>(T packet, ReadOnlyMemory<byte> packetPayloadBytes, string packetName)
    {
        using (
            _logger?.BeginScope(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["@Packet"] = packet,
                    ["PacketPayload"] = packetPayloadBytes.ToArray(),
                }
            )
        )
        {
            _logger?.LogPacketTransmission("Controller", "Host", packetName);
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public IDisposable Subscribe(IMessageSink sink) => _sinks.Subscribe(sink);

    private void PublishMessage<T>(in T message)
        where T : allows ref struct => _sinks.Publish(message);

    public void Dispose()
    {
        _transportGone.Dispose();
        _packetInFlightSemaphore.Dispose();
    }
}
