using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

[MessageSource]
internal sealed partial class HciHostGattServerPeer : GattServerPeer, IAclConnection
{
    private const ushort DefaultAttMtu = 23;

    private readonly L2CapAssembler _assembler;
    private readonly IDisposable _hostSubscription;
    private readonly IDisposable _assemblerSubscription;

    public Hci.HciHost Host { get; }

    ILogger IAclConnection.Logger => base.Logger;
    public ushort ConnectionHandle { get; }
    public ushort AttMtu { get; private set; } = DefaultAttMtu;
    public IAclPacketQueue AclPacketQueue => Host.AclPacketQueue;
    public IL2CapAssembler L2CapAssembler => _assembler;

    public HciHostGattServerPeer(
        HciHostBleCentral central,
        Hci.HciHost host,
        HciLeEnhancedConnectionCompleteV1Event connectionCompleteEvent,
        BleAddress address,
        ILogger<HciHostGattServerPeer> logger
    )
        : base(central, address, logger)
    {
        Host = host;
        ConnectionHandle = connectionCompleteEvent.ConnectionHandle;
        _assembler = new L2CapAssembler(
            Host,
            connectionCompleteEvent.ConnectionHandle,
            ServiceProvider.GetLogger<L2CapAssembler>()
        );
        _assemblerSubscription = _assembler.Subscribe(this);
        _hostSubscription = Host.Subscribe(this);
        Host.RegisterConnection(this);
    }

    [MessageSink]
    private void OnDisconnectEvent(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != ConnectionHandle)
            return;
        ConnectionSubject.OnNext(ConnectionStatus.Disconnected);
    }

    [MessageSink]
    private void OnHciLeDataLengthChangeEvent(HciLeDataLengthChangeEvent hciEvent)
    {
        Logger.LogTrace("Received event about changed data length: {@Event}", hciEvent);
    }

    protected override IObservable<IGattServerService> DiscoverServicesCore()
    {
        return Observable.Create<IGattServerService>(
            async (observer, token) =>
            {
                ushort startingHandle = 0x0001;
                while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
                {
                    AttResponse<AttReadByGroupTypeRsp<ushort>> response = await this.QueryAttPduAsync<
                        AttReadByGroupTypeReq<ushort>,
                        AttReadByGroupTypeRsp<ushort>
                    >(
                            new AttReadByGroupTypeReq<ushort>
                            {
                                StartingHandle = startingHandle,
                                EndingHandle = 0xFFFF,
                                AttributeGroupType = 0x2800, // TODO discover both primary and secondary services
                            },
                            cancellationToken: token
                        )
                        .ConfigureAwait(false);
                    if (response.IsError)
                    {
                        if (response.Error.ErrorCode is AttErrorCode.AttributeNotFoundError)
                            break;
                        throw new Exception($"Could not discover services due to error {response.Error.ErrorCode}");
                    }
                    AttReadByGroupTypeRsp<ushort> rsp = response.Value;
                    if (rsp.AttributeDataList.Length == 0)
                        break;
                    foreach ((ushort handle, ushort endGroup, ushort value) in rsp.AttributeDataList)
                    {
                        observer.OnNext(
                            new HciHostGattServerService(
                                value,
                                GattServiceType.Primary,
                                handle,
                                endGroup,
                                this,
                                ServiceProvider.GetLogger<HciHostGattServerService>()
                            )
                        );
                    }
                    startingHandle = rsp.AttributeDataList[^1].EndGroup;
                }
            }
        );
    }

    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid)
    {
        return Observable.FromAsync<IGattServerService>(async token =>
        {
            ushort startingHandle = 0x0001;
            while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
            {
                AttResponse<AttFindByTypeValueRsp> response = await this.QueryAttPduAsync<
                    AttFindByTypeValueReq,
                    AttFindByTypeValueRsp
                >(
                        new AttFindByTypeValueReq
                        {
                            StartingHandle = startingHandle,
                            EndingHandle = 0xFFFF,
                            AttributeType = 0x2800, // TODO discover both primary and secondary services
                            AttributeValue = uuid.Value.ToByteArray().AsMemory()[..2], // TODO Don't treat all uuids as 16 bit uuids
                        },
                        cancellationToken: token
                    )
                    .ConfigureAwait(false);
                if (response.IsError)
                {
                    if (response.Error.ErrorCode is AttErrorCode.AttributeNotFoundError)
                        break;
                    throw new Exception($"Could not discover services due to error {response.Error.ErrorCode}");
                }
                AttFindByTypeValueRsp rsp = response.Value;
                if (rsp.HandlesInformationList.Length == 0)
                    break;
                foreach ((ushort handle, ushort endGroup) in rsp.HandlesInformationList)
                {
                    return new HciHostGattServerService(
                        uuid,
                        GattServiceType.Primary,
                        handle,
                        endGroup,
                        this,
                        ServiceProvider.GetLogger<HciHostGattServerService>()
                    );
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
            await Host.QueryCommandCompletionAsync<HciLeSetDataLengthCommand, HciLeSetDataLengthResult>(
                    new HciLeSetDataLengthCommand
                    {
                        ConnectionHandle = ConnectionHandle,
                        TxOctets = txOctets,
                        TxTime = txTime,
                    },
                    cancellationToken: token
                )
                .ConfigureAwait(false);
            return this;
        });
    }

    public IObservable<HciHostGattServerPeer> RequestExchangeMtu(ushort mtu)
    {
        return Observable.FromAsync<HciHostGattServerPeer>(async token =>
        {
            AttResponse<AttExchangeMtuRsp> response = await this.QueryAttPduAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
                    new AttExchangeMtuReq { ClientRxMtu = mtu },
                    cancellationToken: token
                )
                .ConfigureAwait(false);
            if (response.IsError)
            {
                throw new Exception($"Could not exchange mtu due to error {response.Error.ErrorCode}");
            }
            AttMtu = Math.Min(mtu, response.Value.ServerRxMtu);
            Logger.LogInformation(
                "Mtu updated to min of {AttMtu} with maximum possible {ServerRxMtu}",
                AttMtu,
                response.Value.ServerRxMtu
            );
            return this;
        });
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        Task<ConnectionStatus> task = WhenConnectionStatusChanged
            .Where(x => x == ConnectionStatus.Disconnected)
            .FirstAsync()
            .ToTask();
        HciCommandStatus status = await Host.QueryCommandStatusAsync(
                new HciDisconnectCommand
                {
                    ConnectionHandle = ConnectionHandle,
                    Reason = HciCommandStatus.RemoteUserTerminatedConnection,
                }
            )
            .ConfigureAwait(false);
        if (status is HciCommandStatus.Success)
        {
            await task.ConfigureAwait(false);
            await Task.Delay(200).ConfigureAwait(false);
        }
        _assembler.Dispose();
        _assemblerSubscription.Dispose();
        _hostSubscription.Dispose();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
