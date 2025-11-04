using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed partial class HciHostGattServerPeer : GattServerPeer
{
    private const ushort DefaultAttMtu = 23;

    private readonly IDisposable _hostSubscription;

    public ushort ConnectionHandle { get; }
    public ushort AttMtu { get; private set; } = DefaultAttMtu;
    public AclConnection Connection { get; }

    public HciHostGattServerPeer(
        HciHostBleCentral central,
        AclConnection connection,
        BleAddress address,
        ILogger<HciHostGattServerPeer> logger
    )
        : base(central, address, logger)
    {
        Connection = connection;
        ConnectionHandle = connection.ConnectionHandle;
        _hostSubscription = connection.Device.Host.Subscribe(this);
    }

    [MessageSink]
    private void OnDisconnectEvent(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != ConnectionHandle)
            return;
        Logger.LogDebug(
            "Received disconnection event for connection 0x{ConnectionHandle:X}. Reason: {Reason}",
            hciEvent.ConnectionHandle,
            hciEvent.Reason
        );
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
                    AttResponse<AttReadByGroupTypeRsp> response = await Connection
                        .QueryAttPduAsync<AttReadByGroupTypeReq<ushort>, AttReadByGroupTypeRsp>(
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
                    AttReadByGroupTypeRsp rsp = response.Value;
                    if (rsp.AttributeDataList.Length == 0)
                        break;
                    foreach ((ushort handle, ushort endGroup, ReadOnlyMemory<byte> value) in rsp.AttributeDataList)
                    {
                        observer.OnNext(
                            new HciHostGattServerService(
                                BleUuid.Read(value.Span),
                                GattServiceType.Primary,
                                handle,
                                endGroup,
                                this,
                                ServiceProvider.GetLogger<HciHostGattServerService>()
                            )
                        );
                    }
                    ushort endGroupHandle = rsp.AttributeDataList[^1].EndGroup;
                    if (endGroupHandle is 0xFFFF)
                        break;
                    startingHandle = (ushort)(rsp.AttributeDataList[^1].EndGroup + 1);
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
                AttResponse<AttFindByTypeValueRsp> response = await Connection
                    .QueryAttPduAsync<AttFindByTypeValueReq, AttFindByTypeValueRsp>(
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
                foreach ((ushort handle, ushort endGroupHandle) in rsp.HandlesInformationList)
                {
                    if (handle < startingHandle || endGroupHandle < handle)
                    {
                        throw new Exception("Could not discover services. Handle values do not make sense");
                    }
                    return new HciHostGattServerService(
                        uuid,
                        GattServiceType.Primary,
                        handle,
                        endGroupHandle,
                        this,
                        ServiceProvider.GetLogger<HciHostGattServerService>()
                    );
                }
                startingHandle = (ushort)(rsp.HandlesInformationList[^1].GroupEndHandle + 1);
            }
            throw new Exception("Could not find a service");
        });
    }

    public Task SetDataLengthAsync(ushort txOctets, ushort txTime, CancellationToken token = default) =>
        Connection.SetDataLengthAsync(txOctets, txTime, token);

    public Task<HciLeReadPhyResult> ReadPhyAsync(CancellationToken token = default) => Connection.ReadPhyAsync(token);

    public async Task RequestExchangeMtuAsync(ushort mtu, CancellationToken token = default)
    {
        AttResponse<AttExchangeMtuRsp> response = await Connection
            .QueryAttPduAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
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
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        // Suppress throwing here
        await Connection
            .DisconnectAsync()
            .ContinueWith(_ => { }, TaskScheduler.Default)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _hostSubscription.Dispose();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
