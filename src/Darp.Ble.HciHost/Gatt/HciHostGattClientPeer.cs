using System.Buffers.Binary;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt;

internal sealed partial class HciHostGattClientPeer : GattClientPeer, IAclConnection, IDisposable
{
    private const ushort MaxMtu = 517;
    private const ushort GattMaxAttributeValueSize = 512;

    private readonly L2CapAssembler _assembler;
    private readonly IDisposable _hostSubscription;
    private readonly IDisposable _assemblerSubscription;

    public ushort ConnectionHandle { get; }
    public new HciHostBlePeripheral Peripheral { get; }
    public Hci.HciHost Host => Peripheral.Device.Host;
    public ushort AttMtu { get; private set; } = 23;
    public IAclPacketQueue AclPacketQueue => Host.AclPacketQueue;
    public IL2CapAssembler L2CapAssembler => _assembler;
    private readonly BehaviorSubject<bool> _disconnectedBehavior = new(value: false);

    ILogger IAclConnection.Logger => base.Logger;

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
        _assembler = new L2CapAssembler(Host, connectionHandle, LoggerFactory.CreateLogger<L2CapAssembler>());
        _assemblerSubscription = _assembler.Subscribe(this);
        _hostSubscription = Host.Subscribe(this);
        Host.RegisterConnection(this);
    }

    [MessageSink]
    private void HandleDisconnectionEvent(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != ConnectionHandle)
            return;
        Logger.LogDebug("Received disconnection event for connection 0x{ConnectionHandle}", hciEvent.ConnectionHandle);
        _disconnectedBehavior.OnNext(value: true);
    }

    [MessageSink]
    private void HandleExchangeMtuRequest(AttExchangeMtuReq request)
    {
        ushort newMtu = Math.Min(request.ClientRxMtu, MaxMtu);
        AttMtu = newMtu;
        this.EnqueueGattPacket(new AttExchangeMtuRsp { ServerRxMtu = newMtu });
    }

    [MessageSink]
    private void HandlePhyUpdateComplete(HciLePhyUpdateCompleteEvent hciEvent)
    {
        Logger.LogDebug(
            "Phy update for connection {ConnectionHandle:X4} with {Status}. Tx: {TxPhy}, Rx: {TxPhy}",
            hciEvent.ConnectionHandle,
            hciEvent.Status,
            hciEvent.TxPhy,
            hciEvent.RxPhy
        );
    }

    [MessageSink]
    private void HandleDataLengthChangeEvent(HciLeDataLengthChangeEvent hciEvent)
    {
        Logger.LogDebug("Received le datachange event: {@PacketData}", hciEvent);
    }

    [MessageSink]
    private async void HandleTypeRequest(AttReadByTypeReq<ushort> request)
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
            ReadOnlyMemory<byte> value = await attribute.ReadValueAsync(this).ConfigureAwait(false);
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

    [MessageSink]
    private async void HandleGroupTypeRequest(AttReadByGroupTypeReq<ushort> attribute)
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
        int maxNumberOfAttributes = availablePduSpace / 6;
        AttGroupTypeData<ushort>[] serviceAttributes = await Peripheral
            .GattDatabase.GetServiceEntries(attribute.StartingHandle)
            .Where(x =>
                x.AttributeType.Equals(attributeType)
                && x.Handle >= attribute.StartingHandle
                && x.Handle <= attribute.EndingHandle
            )
            .ToAsyncEnumerable()
            .SelectAwait(async x => new AttGroupTypeData<ushort>
            {
                Value = BinaryPrimitives.ReadUInt16LittleEndian(await x.ReadValueAsync(this).ConfigureAwait(false)),
                Handle = x.Handle,
                EndGroup = x.EndGroupHandle,
            })
            .Take(maxNumberOfAttributes)
            .ToArrayAsync()
            .ConfigureAwait(false);
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

    [MessageSink]
    private async void HandleReadRequest(AttReadReq request)
    {
        if (!Peripheral.GattDatabase.TryGetAttribute(request.AttributeHandle, out IGattAttribute? attribute))
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);
            return;
        }

        PermissionCheckStatus permissionStatus = attribute.CheckReadPermissions(this);
        if (permissionStatus is not PermissionCheckStatus.Success)
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, (AttErrorCode)permissionStatus);
            return;
        }

        byte[] value = await attribute.ReadValueAsync(this).ConfigureAwait(false);
        int length = Math.Min(AttMtu - 1, value.Length);
        var rsp = new AttReadRsp { AttributeValue = value.AsMemory()[..length] };
        this.EnqueueGattPacket(rsp);
    }

    [MessageSink]
    private void HandleFindInformationRequest(AttFindInformationReq request)
    {
        if (request.StartingHandle is 0 || request.EndingHandle < request.StartingHandle)
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.StartingHandle, AttErrorCode.InvalidHandle);
            return;
        }

        int availablePduSpace = AttMtu - 2;

        List<AttFindInformationData> attributes = [];
        byte? attributeLength = null;
        foreach (
            GattDatabaseEntry attribute in Peripheral.GattDatabase.Where(x =>
                x.Handle >= request.StartingHandle && x.Handle <= request.EndingHandle
            )
        )
        {
            ReadOnlyMemory<byte> attributeTypeBytes = attribute.AttributeType.ToByteArray();
            attributeLength ??= (byte)attributeTypeBytes.Length;

            // Stop adding attributes if this attribute type has a different size
            if (attributeTypeBytes.Length != attributeLength)
                break;
            var entryLength = (byte)(2 + attributeLength);
            // Check if there is enough space to hold this attribute
            if (availablePduSpace < entryLength)
                break;
            availablePduSpace -= entryLength;
            attributes.Add(new AttFindInformationData(attribute.Handle, attributeTypeBytes));
        }
        if (attributes.Count == 0 || attributeLength is null)
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.StartingHandle, AttErrorCode.AttributeNotFoundError);
            return;
        }
        var response = new AttFindInformationRsp
        {
            Format = attributeLength is 2
                ? AttFindInformationFormat.HandleAnd16BitUuid
                : AttFindInformationFormat.HandleAnd128BitUuid,
            InformationData = attributes.ToArray(),
        };
        this.EnqueueGattPacket(response);
    }

    [MessageSink]
    private async void HandleAttWriteRequest(AttWriteReq request)
    {
        if (!Peripheral.GattDatabase.TryGetAttribute(request.AttributeHandle, out IGattAttribute? attribute))
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);
            return;
        }
        if (request.AttributeValue.Length > GattMaxAttributeValueSize)
        {
            this.EnqueueGattErrorResponse(
                request.OpCode,
                request.AttributeHandle,
                AttErrorCode.InvalidAttributeLengthError
            );
            return;
        }

        PermissionCheckStatus permissionStatus = attribute.CheckWritePermissions(this);
        if (permissionStatus is not PermissionCheckStatus.Success)
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, (AttErrorCode)permissionStatus);
            return;
        }

        GattProtocolStatus status = await attribute
            .WriteValueAsync(this, request.AttributeValue.ToArray())
            .ConfigureAwait(false);
        if (status is not GattProtocolStatus.Success)
        {
            this.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, (AttErrorCode)status);
            return;
        }
        this.EnqueueGattPacket(new AttWriteRsp());
    }

    public override bool IsConnected => !_disconnectedBehavior.Value;
    public override IObservable<Unit> WhenDisconnected => _disconnectedBehavior.Where(x => x).Select(_ => Unit.Default);

    public void Dispose()
    {
        _disconnectedBehavior.Dispose();
        _assembler.Dispose();
        _assemblerSubscription.Dispose();
        _hostSubscription.Dispose();
    }
}
