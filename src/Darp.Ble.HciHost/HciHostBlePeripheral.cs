using System.Buffers.Binary;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public new HciHostBlePeripheral Peripheral { get; }
    public Hci.HciHost Host => Peripheral.Device.Host;
    public ushort AttMtu { get; private set; } = 23;
    public AclPacketQueue AclPacketQueue => Host.AclPacketQueue;
    private readonly BehaviorSubject<bool> _disconnectedBehavior = new(value: false);

    public IRefObservable<L2CapPdu> WhenL2CapPduReceived { get; }
    ILogger IBleConnection.Logger => base.Logger;

    public HciHostGattClientPeer(
        HciHostBlePeripheral peripheral,
        BleAddress address,
        ushort connectionHandle,
        ILogger<HciHostGattClientPeer> logger
    )
        : base(peripheral, address, logger)
    {
        ConnectionHandle = connectionHandle;
        Peripheral = peripheral;
        Host.WhenHciEventReceived.SelectWhereEvent<HciDisconnectionCompleteEvent>()
            .Select(_ => true)
            .AsObservable()
            .Do(_ =>
                Logger.LogDebug("Received disconnection event for connection 0x{ConnectionHandle}", ConnectionHandle)
            )
            .Subscribe(_disconnectedBehavior);
        WhenL2CapPduReceived = this.AssembleL2CAp(logger)
            .Where(x => x.ChannelId is 0x0004)
            .TakeUntil(WhenDisconnected)
            .Share();
        WhenL2CapPduReceived
            .SelectWhereAttPdu<AttExchangeMtuReq>()
            .Subscribe(mtuRequest =>
            {
                ushort newMtu = Math.Min(mtuRequest.ClientRxMtu, MaxMtu);
                AttMtu = newMtu;
                this.EnqueueGattPacket(new AttExchangeMtuRsp { ServerRxMtu = newMtu });
            });
        WhenL2CapPduReceived.SelectWhereAttPdu<AttReadReq>().Subscribe(HandleReadRequest);
        WhenL2CapPduReceived.SelectWhereAttPdu<AttReadByGroupTypeReq<ushort>>().Subscribe(HandleGroupTypeRequest);
        WhenL2CapPduReceived.SelectWhereAttPdu<AttReadByTypeReq<ushort>>().Subscribe(HandleTypeRequest);
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
                Logger.LogDebug("Received le datachange event: {@PacketData}", packet.Data);
            });
    }

    private void HandleTypeRequest(AttReadByTypeReq<ushort> request)
    {
        BleUuid attributeType = BleUuid.FromUInt16(request.AttributeType);
        int availablePduSpace = AttMtu - 2;
        int maxAttributeSize = Math.Min(AttMtu - 4, 253);

        List<AttReadByTypeData> attributes = [];
        byte? attributeLength = null;
        foreach (
            GattDatabaseEntry attribute in Peripheral.GattDatabase.Where(x =>
                x.AttributeType.Equals(attributeType)
                && x.Handle >= request.StartingHandle
                && x.Handle <= request.EndingHandle
            )
        )
        {
            ReadOnlyMemory<byte> value = attribute.AttributeValue;
            if (value.Length > maxAttributeSize)
            {
                value = value[..maxAttributeSize];
            }
            attributeLength ??= (byte)value.Length;
            // Only return attributes of same size
            if (value.Length != attributeLength)
                break;
            var entryLength = (byte)(2 + attributeLength);
            // Check if there is enough space to hold this attribute
            if (availablePduSpace < entryLength)
                break;
            attributes.Add(new AttReadByTypeData { Handle = attribute.Handle, Value = value });
        }
        if (attributes.Count == 0)
        {
            var response = new AttErrorRsp
            {
                RequestOpCode = request.OpCode,
                Handle = request.StartingHandle,
                ErrorCode = AttErrorCode.AttributeNotFoundError,
            };
            this.EnqueueGattPacket(response);
            return;
        }
        var rsp = new AttReadByTypeRsp
        {
            Length = (byte)attributes[0].GetByteCount(),
            AttributeDataList = attributes.ToArray(),
        };
        this.EnqueueGattPacket(rsp);
    }

    private void HandleGroupTypeRequest(AttReadByGroupTypeReq<ushort> attribute)
    {
        if (attribute.AttributeGroupType is not (0x2800 or 0x2801))
        {
            var response = new AttErrorRsp
            {
                RequestOpCode = attribute.OpCode,
                Handle = attribute.StartingHandle,
                ErrorCode = AttErrorCode.UnsupportedGroupTypeError,
            };
            this.EnqueueGattPacket(response);
        }

        BleUuid attributeType = BleUuid.FromUInt16(attribute.AttributeGroupType);

        int availablePduSpace = AttMtu - 2;
        var maxNumberOfAttributes = availablePduSpace / 6;
        AttGroupTypeData<ushort>[] serviceAttributes = Peripheral
            .GattDatabase.GetServiceEntries(attribute.StartingHandle)
            .Where(x =>
                x.AttributeType.Equals(attributeType)
                && x.Handle >= attribute.StartingHandle
                && x.Handle <= attribute.EndingHandle
            )
            .Select(x => new AttGroupTypeData<ushort>
            {
                Value = BinaryPrimitives.ReadUInt16LittleEndian(x.AttributeValue),
                Handle = x.Handle,
                EndGroup = x.EndGroupHandle,
            })
            .Take(maxNumberOfAttributes)
            .ToArray();
        if (serviceAttributes.Length == 0)
        {
            var response = new AttErrorRsp
            {
                RequestOpCode = attribute.OpCode,
                Handle = attribute.StartingHandle,
                ErrorCode = AttErrorCode.AttributeNotFoundError,
            };
            this.EnqueueGattPacket(response);
            return;
        }
        var rsp = new AttReadByGroupTypeRsp<ushort>
        {
            Length = (byte)serviceAttributes[0].GetByteCount(),
            AttributeDataList = serviceAttributes,
        };
        this.EnqueueGattPacket(rsp);
    }

    private void HandleReadRequest(AttReadReq request)
    {
        if (!Peripheral.GattDatabase.TryGetAttribute(request.AttributeHandle, out IGattAttribute? attribute))
        {
            var invalidHandleResponse = new AttErrorRsp
            {
                RequestOpCode = request.OpCode,
                Handle = request.AttributeHandle,
                ErrorCode = AttErrorCode.InvalidHandle,
            };
            this.EnqueueGattPacket(invalidHandleResponse);
            return;
        }
        var rsp = new AttReadRsp { AttributeValue = attribute.AttributeValue };
        this.EnqueueGattPacket(rsp);
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

    protected override GattClientService AddServiceCore(BleUuid uuid, bool isPrimary)
    {
        return new HciHostGattClientService(
            this,
            uuid,
            isPrimary ? GattServiceType.Primary : GattServiceType.Secondary,
            LoggerFactory.CreateLogger<HciHostGattClientService>()
        );
    }
}
