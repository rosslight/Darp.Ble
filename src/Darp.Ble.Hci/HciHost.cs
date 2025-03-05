using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Reactive;
using Darp.Ble.Hci.Transport;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public interface IAclConnection : IMessageSinkProvider, IAsyncDisposable
{
    HciHost Host { get; }
    ushort ConnectionHandle { get; }
    ushort AttMtu { get; }
    IAclPacketQueue AclPacketQueue { get; }
    IL2CapAssembler L2CapAssembler { get; }
    protected internal ILogger Logger { get; }
}

public static class ReactiveEx
{
    /// <summary> Share the connection between subscribers. Subscribes once and notifies all current subscribers </summary>
    /// <param name="source"> The source to share </param>
    /// <typeparam name="T">  The type of the elements in the source sequence. </typeparam>
    /// <returns> A shared observable sequence </returns>
    public static IRefObservable<T> Share<T>(this IRefObservable<T> source)
        where T : allows ref struct
    {
        List<IRefObserver<T>> observers = [];
        source.Subscribe(
            value =>
            {
                // Reversed for loop if one of the observers disconnects
                for (int index = observers.Count - 1; index >= 0; index--)
                {
                    IRefObserver<T> refObserver = observers[index];
                    refObserver.OnNext(value);
                }
            },
            exception =>
            {
                // Reversed for loop if one of the observers disconnects
                for (int index = observers.Count - 1; index >= 0; index--)
                {
                    IRefObserver<T> refObserver = observers[index];
                    refObserver.OnError(exception);
                }
            },
            () =>
            {
                // Reversed for loop if one of the observers disconnects
                for (int index = observers.Count - 1; index >= 0; index--)
                {
                    IRefObserver<T> refObserver = observers[index];
                    refObserver.OnCompleted();
                }
            }
        );
        return RefObservable.Create<T, List<IRefObserver<T>>>(
            observers,
            (state, observer) =>
            {
                state.Add(observer);
                return Disposable.Create(
                    (List: state, Observer: observer),
                    state2 => state2.List.Remove(state2.Observer)
                );
            }
        );
    }
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

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    public void EnqueuePacket(IHciPacket packet)
    {
        Logger.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
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
                        break;
                }
                break;
            case { PacketType: HciPacketType.HciAclData }
                when HciAclPacket.TryReadLittleEndian(packet.Pdu, out HciAclPacket x):
                PublishMessage(x);
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
        }
    }

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

    /// <inheritdoc />
    public void Dispose()
    {
        _transportLayer.Dispose();
        _aclPacketQueue?.Dispose();
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

    public static IRefObservable<T> SelectWhereAttPdu<T>(this IRefObservable<L2CapPdu> source)
        where T : IAttPdu, IBinaryReadable<T>
    {
        return source.SelectWhere(
            (L2CapPdu value, [NotNullWhen(true)] out T? result) =>
            {
                if (value.Pdu.Length < 0 || (AttOpCode)value.Pdu[0] != T.ExpectedOpCode)
                {
                    result = default;
                    return false;
                }
                return T.TryReadLittleEndian(value.Pdu, out result);
            }
        );
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
