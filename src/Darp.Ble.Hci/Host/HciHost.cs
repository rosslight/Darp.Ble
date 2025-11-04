using System.Collections.Immutable;
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

/// <summary>
/// The <see cref="HciHost"/> is responsible for all host-related commands.
/// </summary>
/// <param name="transportLayer"></param>
[MessageSource]
public sealed partial class HciHost(HciDevice hciDevice, ITransportLayer transportLayer, ILogger<HciHost>? logger)
    : IDisposable
{
    private readonly ITransportLayer _transportLayer = transportLayer;
    private readonly ILogger<HciHost>? _logger = logger;
    private readonly SemaphoreSlim _packetInFlightSemaphore = new(1);
    private AclPacketQueue? _leAclPacketQueue;
    private bool _isResetDoneAtLeastOnce;

    /// <summary> The HCI Device </summary>
    public HciDevice Device { get; } = hciDevice;

    /// <summary> True, if <see cref="ResetAsync"/> was called at least once. False otherwise </summary>
    [MemberNotNullWhen(true, nameof(_leAclPacketQueue))]
    public bool IsReady => _leAclPacketQueue is not null;

    /// <summary> The ACL Packet queue </summary>
    public IAclPacketQueue AclPacketQueue =>
        _leAclPacketQueue ?? throw new InvalidOperationException("Not initialized yet");

    /// <summary> Resets the controller and configures everything as needed </summary>
    public async Task ResetAsync(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Device.IsDisposed, this);
        await _transportLayer.InitializeAsync(OnReceivedPacket, token).ConfigureAwait(false);
        Activity? activity = Logging.StartInitializeHciHostActivity();
        try
        {
            // Reset the controller
            await this.QueryResetAsync(token).ConfigureAwait(false);
            _isResetDoneAtLeastOnce = true;

            // Read information about the controller
            HciReadLocalSupportedCommandsResult supportedCommandsResult =
                await this.QueryReadLocalSupportedCommandsAsync(token).ConfigureAwait(false);
            ImmutableArray<HciOpCode> x = supportedCommandsResult.SupportedCommands.ToImmutableArray();
            await this.QueryLeReadLocalSupportedFeaturesAsync(token).ConfigureAwait(false);
            HciReadLocalVersionInformationResult version = await this.QueryReadLocalVersionInformationAsync(token)
                .ConfigureAwait(false);
            if (version.HciVersion < CoreVersion.BluetoothCoreSpecification42)
            {
                throw new NotSupportedException(
                    $"Controller version {version.HciVersion} is not supported. Minimum required version is 4.2"
                );
            }

            await this.QueryReadLocalSupportedFeaturesAsync(token).ConfigureAwait(false);

            await this.QuerySetEventMaskAsync(new HciSetEventMaskCommand((EventMask)0x3dbff807bffb9fff), token)
                .ConfigureAwait(false);
            await this.QueryLeSetEventMaskAsync(new HciLeSetEventMaskCommand((LeEventMask)0x00000007fff7ffff), token)
                .ConfigureAwait(false);
            var data = await this.QueryLeReadBufferSizeV1Async(token).ConfigureAwait(false);
            await this.QueryLeReadSuggestedDefaultDataLengthAsync(token).ConfigureAwait(false);
            await this.QueryLeWriteSuggestedDefaultDataLengthAsync(
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
        ObjectDisposedException.ThrowIf(Device.IsDisposed, this);
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

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    internal void EnqueuePacket<T>(HciCommandPacket<T> packet)
        where T : IHciCommand
    {
        using (_logger?.ForContext("PacketPayload", packet.ToArrayLittleEndian()))
        using (_logger?.ForContext("@Packet", packet.Data))
        {
            _logger?.LogPacketTransmission("Host", "Controller", $"{T.OpCode.ToString().ToUpperInvariant()}_COMMAND");
        }
        _transportLayer.Enqueue(packet);
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
                PublishMessage(evt);
                break;
            case HciEventCode.HCI_Disconnection_Complete
                when HciDisconnectionCompleteEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                PublishMessage(evt);
                Device.RemoveConnection(evt.ConnectionHandle);
                break;
            case HciEventCode.HCI_Command_Status
                when HciCommandStatusEvent.TryReadLittleEndian(packet.DataBytes.Span, out var evt):
                LogEventPacketControllerToHost(evt, packet.DataBytes);
                PublishMessage(evt);
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
    public void Dispose()
    {
        _packetInFlightSemaphore.Dispose();
    }
}
