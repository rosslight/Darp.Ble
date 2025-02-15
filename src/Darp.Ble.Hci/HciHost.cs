using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.BinaryObjects;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Reactive;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public sealed class AclPacketQueue
{
    private readonly ITransportLayer _transportLayer;
    private readonly int _maxPacketsInFlight;
    private readonly ConcurrentQueue<IHciPacket> _packetQueue = [];
    private int _packetsInFlight;

    public ushort MaxPacketSize { get; }

    internal AclPacketQueue(HciHost host, ITransportLayer transportLayer, ushort maxPacketSize, int maxPacketsInFlight)
    {
        _transportLayer = transportLayer;
        MaxPacketSize = maxPacketSize;
        _maxPacketsInFlight = maxPacketsInFlight;
        host.WhenHciNumberOfCompletedPacketsEventReceived.SelectMany(x => x.Data.Handles)
            .Subscribe(x =>
            {
                _packetsInFlight -= x.NumCompletedPackets;
                CheckQueue();
            });
    }

    /// <summary>
    /// Enqueue a new
    /// </summary>
    /// <param name="hciPacket"></param>
    internal void Enqueue(IHciPacket hciPacket)
    {
        _packetQueue.Enqueue(hciPacket);
        CheckQueue();
    }

    /// <summary> Checks the number of packets in flight and send as many as possible </summary>
    private void CheckQueue()
    {
        while (_packetsInFlight < _maxPacketsInFlight && _packetQueue.TryDequeue(out IHciPacket? packet))
        {
            _transportLayer.Enqueue(packet);
            _packetsInFlight++;
        }
    }
}

public interface IBleConnection
{
    HciHost Host { get; }
    ushort ConnectionHandle { get; }
    ushort AttMtu { get; }
    AclPacketQueue AclPacketQueue { get; }
    IRefObservable<L2CapPdu> WhenL2CapPduReceived { get; }
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

/// <summary> The HCI Host </summary>
public sealed class HciHost : IDisposable
{
    private readonly ITransportLayer _transportLayer;
    internal ILogger Logger { get; }
    private AclPacketQueue? _aclPacketQueue;
    public AclPacketQueue AclPacketQueue => _aclPacketQueue ?? throw new Exception("Not initialized yet");
    private readonly ConcurrentDictionary<ushort, IBleConnection> _connections = [];

    /// <summary> Initializes a new host with a given transport layer and an optional logger </summary>
    /// <param name="transportLayer"> The transport layer </param>
    /// <param name="logger"> An optional logger </param>
    public HciHost(ITransportLayer transportLayer, ILogger<HciHost> logger)
    {
        _transportLayer = transportLayer;
        Logger = logger;
        WhenHciPacketReceived = _transportLayer.AsRefObservable();
        WhenHciEventReceived = SelectPacket<HciEventPacket>(WhenHciPacketReceived).Share();
        WhenHciAclPacketReceived = SelectPacket<HciAclPacket>(WhenHciPacketReceived).Share();
        WhenHciLeMetaEventReceived = WhenHciEventReceived.SelectWhereEvent<HciLeMetaEvent>().Share();
        WhenHciNumberOfCompletedPacketsEventReceived = WhenHciEventReceived
            .SelectWhereEvent<HciNumberOfCompletedPacketsEvent>()
            .Share();
    }

    private static IRefObservable<TPacket> SelectPacket<TPacket>(IRefObservable<HciPacket> source)
        where TPacket : IHciPacket<TPacket>, IBinaryReadable<TPacket>
    {
        return source.SelectWhere(
            (HciPacket packet, [NotNullWhen(true)] out TPacket? eventPacket) =>
            {
                if (packet.PacketType == TPacket.Type && TPacket.TryReadLittleEndian(packet.Pdu, out eventPacket))
                {
                    return true;
                }
                eventPacket = default;
                return false;
            }
        );
    }

    /// <summary> Observable sequence of <see cref="IHciPacket"/> emitted when an HCI packet is received. </summary>
    public IRefObservable<HciPacket> WhenHciPacketReceived { get; }

    /// <summary> Observable sequence of <see cref="HciEventPacket"/> emitted when an HCI event packet is received. </summary>
    public IRefObservable<HciEventPacket> WhenHciEventReceived { get; }

    /// <summary> Observable sequence of <see cref="HciEventPacket{HciLeMetaEvent}"/> emitted when an HCI event packet with <see cref="HciLeMetaEvent"/> is received. </summary>
    public IRefObservable<HciEventPacket<HciLeMetaEvent>> WhenHciLeMetaEventReceived { get; }

    /// <summary> Observable sequence of <see cref="HciEventPacket{HciLeMetaEvent}"/> emitted when an HCI event packet with <see cref="HciNumberOfCompletedPacketsEvent"/> is received. </summary>
    public IRefObservable<
        HciEventPacket<HciNumberOfCompletedPacketsEvent>
    > WhenHciNumberOfCompletedPacketsEventReceived { get; }

    /// <summary> Observable sequence of <see cref="HciAclPacket"/> emitted when ???. </summary>
    public IRefObservable<HciAclPacket> WhenHciAclPacketReceived { get; }

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    public void EnqueuePacket(IHciPacket packet)
    {
        Logger.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    /// <summary> Initialize the host </summary>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _transportLayer.Initialize();
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
    public void Dispose() => _transportLayer.Dispose();
}

public readonly struct AttResponse<T> : IAttPdu
    where T : IAttPdu
{
    private AttResponse(bool isSuccess, T attValue, AttErrorRsp errorResponse)
    {
        IsSuccess = isSuccess;
        Value = attValue;
        Error = errorResponse;
    }

    public static AttOpCode ExpectedOpCode => T.ExpectedOpCode;

    public T Value { get; }
    public AttErrorRsp Error { get; }

    public bool IsError => !IsSuccess;

    public bool IsSuccess { get; }

    public AttOpCode OpCode => IsSuccess ? Value.OpCode : Error.OpCode;

    public static AttResponse<T> Ok(T attResponse) => new(isSuccess: true, attResponse, errorResponse: default);

    public static AttResponse<T> Fail(AttErrorRsp errorResponse) => new(isSuccess: false, default!, errorResponse);
}

public static class L2CapHelpers
{
    public static IRefObservable<AttResponse<T>> SelectWhereAttPduOrError<T>(this IRefObservable<L2CapPdu> source)
        where T : IAttPdu, IBinaryReadable<T>
    {
        return source.SelectWhere(
            (L2CapPdu value, out AttResponse<T> result) =>
            {
                if (value.Pdu.Length < 0)
                {
                    result = default;
                    return false;
                }
                var opCode = (AttOpCode)value.Pdu[0];

                if (opCode is AttOpCode.ATT_ERROR_RSP)
                {
                    if (!AttErrorRsp.TryReadLittleEndian(value.Pdu, out AttErrorRsp errorResult))
                    {
                        result = default;
                        return false;
                    }
                    result = AttResponse<T>.Fail(errorResult);
                    return true;
                }

                if (opCode != T.ExpectedOpCode || !T.TryReadLittleEndian(value.Pdu, out T? tResult))
                {
                    result = default;
                    return false;
                }
                result = AttResponse<T>.Ok(tResult);
                return true;
            }
        );
    }

    public static async Task<AttResponse<TResponse>> QueryAttPduAsync<TAttRequest, TResponse>(
        this IBleConnection connection,
        TAttRequest request,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default
    )
        where TAttRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(connection);
        IObservable<AttResponse<TResponse>> observable = connection
            .WhenL2CapPduReceived.SelectWhereAttPduOrError<TResponse>()
            .Where(x => x.OpCode == TResponse.ExpectedOpCode || x.OpCode == AttOpCode.ATT_ERROR_RSP)
            .AsObservable();
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
    public static void EnqueueGattPacket<TAttPdu>(this IBleConnection connection, TAttPdu attPdu)
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
        this AclPacketQueue packetQueue,
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
                new HciAclPacket<EncodableByteArray>(
                    connectionHandle,
                    packetBoundaryFlag,
                    broadcastFlag,
                    totalLength,
                    new EncodableByteArray(l2CApBytes)
                )
            );
            packetBoundaryFlag = PacketBoundaryFlag.ContinuingFragment;
            offset += totalLength;
            numberOfRemainingBytes -= totalLength;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="connectionHandle"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/logical-link-control-and-adaptation-protocol-specification.html#UUID-d64de646-624f-768d-b99c-673e686e3590"/>
    public static IRefObservable<L2CapPdu> AssembleL2CAp(this IBleConnection connection, ILogger logger)
    {
        ushort targetLength = 0;
        List<byte> dataBytes = [];
        return RefObservable.Create<L2CapPdu>(observer =>
            connection
                .Host.WhenHciAclPacketReceived.Where(x => x.ConnectionHandle == connection.ConnectionHandle)
                .Subscribe(
                    packet =>
                    {
                        if (packet.PacketBoundaryFlag is PacketBoundaryFlag.FirstAutoFlushable)
                        {
                            if (dataBytes.Count > 0)
                            {
                                logger.LogWarning(
                                    "Got packet {@Packet} but collector still has an open entry: {@Collector}",
                                    packet,
                                    dataBytes
                                );
                                return;
                            }
                            if (packet.DataBytes.Length < 4)
                            {
                                logger.LogWarning(
                                    "Got packet {@Packet} but data bytes are too short for header",
                                    packet
                                );
                                return;
                            }
                            targetLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.DataBytes);
                            dataBytes.AddRange(packet.DataBytes);
                        }
                        else if (packet.PacketBoundaryFlag is PacketBoundaryFlag.ContinuingFragment)
                        {
                            if (targetLength == 0 || dataBytes.Count == 0)
                            {
                                logger.LogWarning(
                                    "Got packet {@Packet} but collector is invalid: {@Collector} / {TargetLength}",
                                    packet,
                                    dataBytes,
                                    targetLength
                                );
                                return;
                            }
                            dataBytes.AddRange(packet.DataBytes);
                        }
                        else
                        {
                            logger.LogWarning("Got unsupported packet boundary flag for packet {@Packet}", packet);
                            return;
                        }

                        if (dataBytes.Count > targetLength + 4)
                        {
                            logger.LogWarning(
                                "Got too many bytes in {@List} after packet {@Packet}",
                                dataBytes,
                                packet
                            );
                            return;
                        }
                        if (dataBytes.Count != targetLength + 4)
                            return;
                        Span<byte> assembledPdu = dataBytes.ToArray().AsSpan();
                        ushort channelId = BinaryPrimitives.ReadUInt16LittleEndian(assembledPdu[2..4]);
                        observer.OnNext(new L2CapPdu(channelId, assembledPdu[4..]));
                        dataBytes.Clear();
                    },
                    observer.OnError,
                    observer.OnCompleted
                )
        );
    }
}
