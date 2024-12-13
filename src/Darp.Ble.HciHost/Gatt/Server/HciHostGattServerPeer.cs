using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Darp.BinaryObjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Microsoft.Extensions.Logging;
using L2CAp = (ushort ChannelId, byte[] Payload);

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerPeer : GattServerPeer
{
    private const ushort DefaultAttMtu = 27;
    private const int MaxNumAclPackagesInFlight = 1;

    private readonly Hci.HciHost _host;
    private readonly List<IDisposable> _disposables = [];
    private readonly ConcurrentQueue<HciAclPacket> _aclPacketQueue = new();

    internal new ILogger? Logger => base.Logger;
    public ushort ConnectionHandle { get; }
    public ushort AttMtu { get; private set; } = DefaultAttMtu;

    private int _aclPacketsInFlight;

    public HciHostGattServerPeer(HciHostBleCentral central,
        Hci.HciHost host,
        HciLeEnhancedConnectionCompleteV1Event connectionCompleteEvent,
        BleAddress address,
        ILogger? logger) : base(central, address, logger)
    {
        _host = host;
        ConnectionHandle = connectionCompleteEvent.ConnectionHandle;
        _host.WhenHciEventPackageReceived
            .Where(x => x.EventCode is HciEventCode.HCI_Disconnection_Complete)
            .SelectWhereEvent<HciDisconnectionCompleteEvent>()
            .Where(x => x.Data.ConnectionHandle == ConnectionHandle)
            .Select(_ => ConnectionStatus.Disconnected)
            .FirstAsync()
            .Subscribe(ConnectionSubject);
        IObservable<L2CAp> whenL2CApReceived = AssembleL2CAp(_host.WhenHciPacketReceived);
        _disposables.Add(_host.WhenHciLeMetaEventPackageReceived
            .SelectWhereLeMetaEvent<HciLeDataLengthChangeEvent>()
            .Subscribe(changeEventPacket =>
            {
                Logger?.LogTrace("Received event about changed data length: {@Event}", changeEventPacket);
            }));
        var subject = new Subject<(AttOpCode OpCode, byte[] Pdu)>();
        WhenAttPduReceived = subject;
        whenL2CApReceived
            .Where(tuple => tuple.Payload.Length > 0)
            .Select(x => ((AttOpCode)x.Payload[0], x.Payload))
            //.Do(x => Logger?.LogTrace("Received {OpCode} {@Payload}", x.Item1, x.Payload))
            .TakeUntil(WhenConnectionStatusChanged.Where(x => x is ConnectionStatus.Disconnected))
            .Subscribe(subject);
        _host.WhenHciEventPackageReceived
            .SelectWhereEvent<HciNumberOfCompletedPacketsEvent>()
            .SelectMany(x => x.Data.Handles)
            .Where(x => x.ConnectionHandle == ConnectionHandle)
            .Subscribe(x =>
            {
                _aclPacketsInFlight -= x.NumCompletedPackets;
                ProcessAclPackagesIfPossible();
            });
    }

    public IObservable<(AttOpCode OpCode, byte[] Pdu)> WhenAttPduReceived { get; }

    protected override IObservable<IGattServerService> DiscoverServicesCore()
    {
        return Observable.Create<IGattServerService>(async (observer, token) =>
        {
            ushort startingHandle = 0x0001;
            while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
            {
                AttReadResult response = await this
                    .QueryAttPduAsync<AttReadByGroupTypeReq<ushort>, AttReadByGroupTypeRsp<ushort>>(new AttReadByGroupTypeReq<ushort>
                    {
                        StartingHandle = startingHandle,
                        EndingHandle = 0xFFFF,
                        AttributeType = 0x2800,
                    }, cancellationToken: token)
                    .ConfigureAwait(false);
                if (response.OpCode is AttOpCode.ATT_ERROR_RSP
                    && AttErrorRsp.TryDecode(response.Pdu, out AttErrorRsp errorRsp, out _))
                {
                    if (errorRsp.ErrorCode is AttErrorCode.AttributeNotFoundError) break;
                    throw new Exception($"Could not discover services due to error {errorRsp.ErrorCode}");
                }
                if (!(response.OpCode is AttOpCode.ATT_READ_BY_GROUP_TYPE_RSP && AttReadByGroupTypeRsp<ushort>
                        .TryDecode(response.Pdu, out AttReadByGroupTypeRsp<ushort> rsp, out _)))
                {
                    throw new Exception($"Received unexpected att response {response.OpCode}");
                }
                if (rsp.AttributeDataList.Length == 0) break;
                foreach ((ushort handle, ushort endGroup, ushort value) in rsp.AttributeDataList)
                {
                    var uuid = new BleUuid(value);
                    observer.OnNext(new HciHostGattServerService(uuid, handle, endGroup, this, Logger));
                }
                startingHandle = rsp.AttributeDataList[^1].EndGroup;
            }
        });
    }

    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid)
    {
        return Observable.FromAsync<IGattServerService>(async token =>
        {
            ushort startingHandle = 0x0001;
            while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
            {
                AttReadResult response = await this
                    .QueryAttPduAsync<AttFindByTypeValueReq, AttFindByTypeValueRsp>(new AttFindByTypeValueReq
                    {
                        StartingHandle = startingHandle,
                        EndingHandle = 0xFFFF,
                        AttributeType = 0x2800,
                        AttributeValue = uuid.Value.ToByteArray()[..2], // TODO Don't treat all uuids as 16 bit uuids
                    }, cancellationToken: token)
                    .ConfigureAwait(false);
                if (response.OpCode is AttOpCode.ATT_ERROR_RSP
                    && AttErrorRsp.TryDecode(response.Pdu, out AttErrorRsp errorRsp, out _))
                {
                    if (errorRsp.ErrorCode is AttErrorCode.AttributeNotFoundError) break;
                    throw new Exception($"Could not discover services due to error {errorRsp.ErrorCode}");
                }
                if (!(response.OpCode is AttOpCode.ATT_FIND_BY_TYPE_VALUE_RSP && AttFindByTypeValueRsp
                        .TryDecode(response.Pdu, out AttFindByTypeValueRsp rsp, out _)))
                {
                    throw new Exception($"Received unexpected att response {response.OpCode}");
                }
                if (rsp.HandlesInformationList.Length == 0) break;
                foreach ((ushort handle, ushort endGroup) in rsp.HandlesInformationList)
                {
                    return new HciHostGattServerService(uuid, handle, endGroup, this, Logger);
                }
                startingHandle = rsp.HandlesInformationList[^1].GroupEndHandle;
            }
            throw new Exception("Could not find a service");
        });
    }

    public IObservable<HciHostGattServerPeer> SetDataLength(ushort txOctets, ushort txTime)
    {
        if (txOctets is < 0x001B or > 0x00FB)
            return Observable.Throw<HciHostGattServerPeer>(new ArgumentOutOfRangeException(nameof(txOctets)));
        if (txTime is < 0x0148 or > 0x4290)
            return Observable.Throw<HciHostGattServerPeer>(new ArgumentOutOfRangeException(nameof(txOctets)));

        return Observable.FromAsync<HciHostGattServerPeer>(async token =>
        {
            await _host.QueryCommandCompletionAsync<HciLeSetDataLengthCommand, HciLeSetDataLengthResult>(new HciLeSetDataLengthCommand
            {
                ConnectionHandle = ConnectionHandle,
                TxOctets = txOctets,
                TxTime = txTime,
            }, cancellationToken: token).ConfigureAwait(false);
            return this;
        });
    }

    public IObservable<HciHostGattServerPeer> RequestExchangeMtu(ushort mtu)
    {
        return Observable.FromAsync<HciHostGattServerPeer>(async token =>
        {
            AttReadResult response = await this.QueryAttPduAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
                new AttExchangeMtuReq { ClientRxMtu = mtu, },
                cancellationToken: token).ConfigureAwait(false);
            if (response.OpCode is AttOpCode.ATT_ERROR_RSP
                && AttErrorRsp.TryDecode(response.Pdu, out AttErrorRsp errorRsp, out _))
            {
                throw new Exception($"Could not exchange mtu due to error {errorRsp.ErrorCode}");
            }

            if (!(response.OpCode is AttOpCode.ATT_EXCHANGE_MTU_RSP && AttExchangeMtuRsp
                    .TryDecode(response.Pdu, out AttExchangeMtuRsp rsp, out _)))
            {
                throw new Exception($"Received unexpected att response {response.OpCode}");
            }
            AttMtu = Math.Min(mtu, rsp.ServerRxMtu);
            Logger?.LogInformation("Mtu updated to min of {AttMtu} with maximum possible {ServerRxMtu}", AttMtu, rsp.ServerRxMtu);
            return this;
        });
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (IsConnected)
        {
            Task<ConnectionStatus> task = WhenConnectionStatusChanged
                .Where(x => x == ConnectionStatus.Disconnected)
                .FirstAsync()
                .ToTask();
            await _host.QueryCommandStatusAsync(new HciDisconnectCommand
            {
                ConnectionHandle = ConnectionHandle,
                Reason = HciCommandStatus.RemoteUserTerminatedConnection,
            }).ConfigureAwait(false);
            await task.ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);
        }
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private void ProcessAclPackagesIfPossible()
    {
        while (_aclPacketsInFlight < MaxNumAclPackagesInFlight)
        {
            if (!_aclPacketQueue.TryDequeue(out HciAclPacket? packet)) return;
            _host.EnqueuePacket(packet);
            _aclPacketsInFlight += 1;
        }
    }

    public IObservable<AttReadResult> QueryAttPduRequest<TAttRequest, TAttResponse>(TAttRequest request)
        where TAttRequest : IAttPdu, IWritable
        where TAttResponse : IAttPdu
    {
        const ushort cId = 0x04;
        return Observable.Create<AttReadResult>(observer =>
        {
            IDisposable disposable = WhenAttPduReceived
                .Where(x => x.OpCode == TAttResponse.ExpectedOpCode || x.OpCode is AttOpCode.ATT_ERROR_RSP)
                .Select(x => new AttReadResult(x.OpCode, x.Pdu))
                .Subscribe(observer);
            EnqueueL2CapBasic(cId, request.ToArrayLittleEndian());
            return disposable;
        });
    }

    public void SendL2CapBasicCommand(ushort channelIdentifier, byte[] payloadBytes)
    {
        EnqueueL2CapBasic(channelIdentifier, payloadBytes);
    }

    private void EnqueueL2CapBasic(ushort channelIdentifier, byte[] payloadBytes)
    {
        ReadOnlySpan<byte> payloadSpan = payloadBytes;
        int numberOfRemainingBytes = 4 + payloadBytes.Length;
        var offset = 0;
        var packetBoundaryFlag = PacketBoundaryFlag.FirstNonAutoFlushable;
        const BroadcastFlag broadcastFlag = BroadcastFlag.PointToPoint;
        while (numberOfRemainingBytes > 0)
        {
            ushort totalLength = Math.Min((ushort)numberOfRemainingBytes, AttMtu);
            var l2CApBytes = new byte[totalLength];
            Span<byte> l2CApSpan = l2CApBytes;
            if (offset < 4)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(l2CApSpan, (ushort)payloadBytes.Length);
                BinaryPrimitives.WriteUInt16LittleEndian(l2CApSpan[2..], channelIdentifier);
                payloadSpan[..(totalLength - 4)].CopyTo(l2CApSpan[4..]);
            }
            else
            {
                payloadSpan[offset..(offset + totalLength)].CopyTo(l2CApSpan);
            }

            _aclPacketQueue.Enqueue(new HciAclPacket<EncodableByteArray>(ConnectionHandle,
                packetBoundaryFlag,
                broadcastFlag,
                totalLength,
                new EncodableByteArray(l2CApBytes)));
            ProcessAclPackagesIfPossible();
            packetBoundaryFlag = PacketBoundaryFlag.ContinuingFragment;
            offset += totalLength;
            numberOfRemainingBytes -= totalLength;
        }
    }

    private IObservable<L2CAp> AssembleL2CAp(IObservable<IHciPacket> source)
    {
        ushort targetLength = default;
        List<byte> dataBytes = [];
        return Observable.Create<L2CAp>(observer => source
            .OfType<HciAclPacket>()
            .Where(x => x.ConnectionHandle == ConnectionHandle)
            .Subscribe(packet =>
            {
                if (packet.PacketBoundaryFlag is PacketBoundaryFlag.FirstAutoFlushable)
                {
                    if (dataBytes.Count > 0)
                    {
                        Logger?.LogWarning("Got packet {@Packet} but collector still has an open entry: {@Collector}", packet, dataBytes);
                        return;
                    }
                    if (packet.DataBytes.Length < 4)
                    {
                        Logger?.LogWarning("Got packet {@Packet} but data bytes are too short for header", packet);
                        return;
                    }
                    targetLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.DataBytes);
                    dataBytes.AddRange(packet.DataBytes);
                }
                else if (packet.PacketBoundaryFlag is PacketBoundaryFlag.ContinuingFragment)
                {
                    if (targetLength == 0 || dataBytes.Count == 0)
                    {
                        Logger?.LogWarning("Got packet {@Packet} but collector is invalid: {@Collector} / {TargetLength}", packet, dataBytes, targetLength);
                        return;
                    }
                    dataBytes.AddRange(packet.DataBytes);
                }
                else
                {
                    Logger?.LogWarning("Got unsupported packet boundary flag for packet {@Packet}", packet);
                    return;
                }

                if (dataBytes.Count > targetLength + 4)
                {
                    Logger?.LogWarning("Got too many bytes in {@List} after packet {@Packet}", dataBytes, packet);
                    return;
                }
                if (dataBytes.Count != targetLength + 4) return;
                observer.OnNext((BinaryPrimitives.ReadUInt16LittleEndian(dataBytes.ToArray().AsSpan()[2..4]),
                    dataBytes.ToArray()[4..]));
                dataBytes.Clear();
            }, observer.OnError, observer.OnCompleted));
    }
}

internal static class Extensions2
{
    public static void SendAttMtuCommand<TAttCommand>(this HciHostGattServerPeer server, TAttCommand request)
        where TAttCommand : IAttPdu, IEncodable
    {
        const ushort cId = 0x04;
        server.SendL2CapBasicCommand(cId, request.ToByteArray());
    }

    public static async Task<AttReadResult> QueryAttPduAsync<TAttRequest, TResponse>(this HciHostGattServerPeer client,
        TAttRequest request, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        where TAttRequest : IAttPdu, IWritable
        where TResponse : IAttPdu
    {
        IObservable<AttReadResult> observable = client.QueryAttPduRequest<TAttRequest, TResponse>(request);
        if (timeout.TotalNanoseconds > 0) observable = observable.Timeout(timeout);
        try
        {
            AttReadResult result = await observable
                .FirstAsync()
                .ToTask(cancellationToken)
                .ConfigureAwait(false);
            client.Logger?.LogTrace("HciClient: Finished att request {@Request} with result {@Result}", request, result);
            return result;
        }
        catch (Exception e)
        {
            client.Logger?.LogWarning(e, "HciClient: Could not finish att request {@Request} because of {Message}", request, e.Message);
            throw;
        }
    }
}