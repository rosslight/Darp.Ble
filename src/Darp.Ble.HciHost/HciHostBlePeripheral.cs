using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.BinaryObjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Reactive;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

internal sealed class HciHostGattClientPeer : GattClientPeer, IBleConnection
{
    public ushort ConnectionHandle { get; }
    private const ushort MaxMtu = 517;
    public Hci.HciHost Host { get; }
    public ushort AttMtu { get; private set; } = 23;
    public AclPacketQueue AclPacketQueue => Host.AclPacketQueue;
    private readonly BehaviorSubject<bool> _disconnectedBehavior = new(value: false);

    public IRefObservable<L2CapPdu> WhenL2CapPduReceived { get; }

    public HciHostGattClientPeer(
        HciHostBlePeripheral peripheral,
        BleAddress address,
        ushort connectionHandle,
        ILogger<HciHostGattClientPeer> logger
    )
        : base(peripheral, address, logger)
    {
        ConnectionHandle = connectionHandle;
        Host = peripheral.Device.Host;
        Host.WhenHciLeMetaEventReceived.SelectWhereEvent<HciDisconnectionCompleteEvent>()
            .Select(_ => true)
            .AsObservable()
            .Subscribe(_disconnectedBehavior);
        WhenL2CapPduReceived = this.AssembleL2CAp(logger)
            .Where(x => x.ChannelId is 0x0004)
            .TakeUntil(WhenDisconnected)
            .Share();
        WhenL2CapPduReceived.Subscribe(pdu =>
        {
            int i = 0;
        });
        WhenL2CapPduReceived
            .SelectWhereAttPdu<AttExchangeMtuReq>()
            .Subscribe(mtuRequest =>
            {
                ushort newMtu = Math.Min(mtuRequest.ClientRxMtu, MaxMtu);
                AttMtu = newMtu;
                this.EnqueueGattPacket(new AttExchangeMtuRsp { ServerRxMtu = newMtu });
            });
        WhenL2CapPduReceived
            .SelectWhereAttPdu<AttFindInformationReq>()
            .Subscribe(x =>
            {
                int i = 0;
            });
        WhenL2CapPduReceived
            .SelectWhereAttPdu<AttReadByGroupTypeReq<ushort>>()
            .Subscribe(x =>
            {
                int i = 0;
            });
        WhenL2CapPduReceived
            .SelectWhereAttPdu<AttReadByTypeReq<ushort>>()
            .Subscribe(x =>
            {
                int i = 0;
            });
        Host.WhenHciLeMetaEventReceived.AsObservable()
            .SelectWhereLeMetaEvent<HciLePhyUpdateCompleteEvent>()
            .Subscribe(packet =>
            {
                Logger.LogDebug(
                    "Phy update for connection {ConnectionHandle:X4} with {Status}. Tx: {TxPhy}, Rx: {TxPhy}",
                    packet.Data.ConnectionHandle,
                    packet.Data.Status,
                    packet.Data.TxPhy,
                    packet.Data.RxPhy
                );
            });
        Host.WhenHciLeMetaEventReceived.AsObservable()
            .SelectWhereLeMetaEvent<HciLeDataLengthChangeEvent>()
            .Subscribe(packet =>
            {
                int i = 0;
            });
    }

    public override bool IsConnected => !_disconnectedBehavior.Value;
    public override IObservable<Unit> WhenDisconnected => _disconnectedBehavior.Where(x => x).Select(_ => Unit.Default);
}

internal sealed class HciHostBlePeripheral : BlePeripheral
{
    private readonly Hci.HciHost _host;

    public HciHostBlePeripheral(HciHostBleDevice device, ILogger<HciHostBlePeripheral> logger)
        : base(device, logger)
    {
        _host = device.Host;
        Device = device;
        _host.WhenHciLeMetaEventReceived.Subscribe(packet =>
        {
            HciLeMetaSubEventType subEventCode = packet.Data.SubEventCode;
            if (
                subEventCode is HciLeMetaSubEventType.HCI_LE_Enhanced_Connection_Complete_V1
                && HciLeEnhancedConnectionCompleteV1Event.TryReadLittleEndian(
                    packet.DataBytes,
                    out HciLeEnhancedConnectionCompleteV1Event connectionCompleteV1Event
                )
            )
            {
                OnHciConnectionCompleteEvent(connectionCompleteV1Event);
            }
        });
    }

    public new HciHostBleDevice Device { get; }

    private void OnHciConnectionCompleteEvent(HciLeEnhancedConnectionCompleteV1Event connectionCompleteEvent)
    {
        if (connectionCompleteEvent.Status is not HciCommandStatus.Success)
        {
            Logger.LogWarning(
                "Received connection request but is failed with status {Status}",
                connectionCompleteEvent.Status
            );
            return;
        }
        var peerDeviceAddress = new BleAddress(
            (BleAddressType)connectionCompleteEvent.PeerAddressType,
            (UInt48)(ulong)connectionCompleteEvent.PeerAddress
        );

        Logger.LogDebug(
            "Connection 0x{Handle:X4} with {PeerAddress} completed",
            connectionCompleteEvent.ConnectionHandle,
            peerDeviceAddress
        );
        if (!PeerDevices.TryGetValue(peerDeviceAddress, out IGattClientPeer? peerDevice))
        {
            peerDevice = new HciHostGattClientPeer(
                this,
                peerDeviceAddress,
                connectionCompleteEvent.ConnectionHandle,
                LoggerFactory.CreateLogger<HciHostGattClientPeer>()
            );
            OnConnectedCentral(peerDevice);
        }
    }

    protected override Task<GattClientService> AddServiceAsyncCore(
        BleUuid uuid,
        bool isPrimary,
        CancellationToken cancellationToken
    )
    {
        var service = new HciHostGattClientService(
            this,
            uuid,
            isPrimary ? GattServiceType.Primary : GattServiceType.Secondary,
            LoggerFactory.CreateLogger<HciHostGattClientService>()
        );
        return Task.FromResult<GattClientService>(service);
    }
}
