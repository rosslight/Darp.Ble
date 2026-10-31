using System.Buffers;
using System.Buffers.Binary;
using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Connections;

/// <summary> This adapter's link to one peripheral </summary>
internal sealed partial class HciHostConnectionDriver : IConnectionDriver
{
    private readonly HciDevice _device;
    private readonly AclConnection _connection;
    private readonly IConnectionSink _sink;
    private readonly Lock _lock = new();

    // One object per attribute, so every discovery returns the same ones. Only discovery, one request at a time,
    // touches services and descriptors; the read loop also looks up characteristics, so those need the lock
    private readonly Dictionary<ushort, HciHostRemoteService> _services = [];
    private readonly Dictionary<ushort, HciHostRemoteCharacteristic> _characteristics = [];
    private readonly Dictionary<ushort, HciHostRemoteDescriptor> _descriptors = [];
    private readonly IDisposable _hostSubscription;
    private readonly IDisposable _assemblerSubscription;
    private const int MinimumMtu = 23;

    // The longest value of an attribute
    private const int MaxValueLength = 512;

    private readonly TaskCompletionSource<DisconnectInfo> _down = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    /// <summary> Takes over the link; called on the read loop right when it came up, so nothing it receives is missed </summary>
    public HciHostConnectionDriver(HciDevice device, AclConnection connection, IConnectionSink sink)
    {
        _device = device;
        _connection = connection;
        _sink = sink;
        Client = new AttClient(connection);
        _hostSubscription = device.Host.Subscribe(this);
        _assemblerSubscription = connection.Assembler.Subscribe(this);
        device.Host.TransportFailed += OnTransportFailed;
    }

    /// <summary> The ATT client of the link </summary>
    public AttClient Client { get; }

    public async Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request)
    {
        // The exchange is optional: a peer that does not support it keeps the default MTU
        AttExchangeMtuRsp? response = await Client
            .QueryUnlessAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
                new AttExchangeMtuReq { ClientRxMtu = (ushort)clientMtu },
                AttErrorCode.RequestNotSupportedError,
                request
            )
            .ConfigureAwait(false);
        if (response is not { } agreed)
            return MinimumMtu;
        var mtu = (ushort)Math.Max(MinimumMtu, Math.Min(clientMtu, agreed.ServerRxMtu));
        _connection.AttMtu = mtu;
        return mtu;
    }

    /// <summary> Reads the primary service declarations page by page </summary>
    public async Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request)
    {
        List<IRemoteServiceDriver> services = [];
        ushort start = 0x0001;
        while (!request.IsAbandoned)
        {
            AttReadByGroupTypeRsp? page = await Client
                .QueryUnlessAsync<AttReadByGroupTypeReq<ushort>, AttReadByGroupTypeRsp>(
                    new AttReadByGroupTypeReq<ushort>
                    {
                        StartingHandle = start,
                        EndingHandle = 0xFFFF,
                        AttributeGroupType = 0x2800,
                    },
                    AttErrorCode.AttributeNotFoundError,
                    request
                )
                .ConfigureAwait(false);
            if (page is not { AttributeDataList.Length: > 0 } response)
                break;
            foreach ((ushort handle, ushort endGroup, ReadOnlyMemory<byte> uuid) in response.AttributeDataList)
                services.Add(GetService(handle, endGroup, BleUuid.Read(uuid.Span)));
            ushort end = response.AttributeDataList[^1].EndGroup;
            if (end is 0xFFFF)
                break;
            start = (ushort)(end + 1);
        }
        return services;
    }

    /// <summary> Sends a write request; the core rejects values longer than one write carries </summary>
    public Task WriteAsync(ushort handle, ReadOnlyMemory<byte> value, GattRequest request) =>
        Client.QueryAsync<AttWriteReq, AttWriteRsp>(
            new AttWriteReq { AttributeHandle = handle, AttributeValue = value },
            request
        );

    /// <summary> Sends a write command, which the peer does not answer </summary>
    public ValueTask WriteCommandAsync(
        ushort handle,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken,
        CancellationToken connectionClosed
    )
    {
        // Completes once the controller has a buffer for the command, which paces a loop of writes
        return _connection.SendGattPacketAsync(
            new AttWriteCmd { Handle = handle, Value = value },
            onHandOff: null,
            cancellationToken,
            connectionClosed
        );
    }

    /// <summary> Reads a value; while answers come back full, the rest follows with Read Blob from where they ended </summary>
    public async Task<byte[]> ReadAsync(ushort handle, GattRequest request)
    {
        AttReadRsp first = await Client
            .QueryAsync<AttReadReq, AttReadRsp>(new AttReadReq { AttributeHandle = handle }, request)
            .ConfigureAwait(false);
        int fullAnswer = _connection.AttMtu - 1;
        // At a large MTU one answer can already hold more than an attribute may
        if (first.AttributeValue.Length < fullAnswer)
            return first.AttributeValue[..Math.Min(first.AttributeValue.Length, MaxValueLength)].ToArray();
        var value = new ArrayBufferWriter<byte>();
        value.Write(first.AttributeValue.Span);
        ReadOnlyMemory<byte> part = first.AttributeValue;
        // A shorter part is the last one; an empty one ends a value that ended where an answer did
        while (part.Length >= fullAnswer && value.WrittenCount < MaxValueLength && !request.IsAbandoned)
        {
            AttReadBlobRsp? blob = await Client
                .QueryUnlessAsync<AttReadBlobReq, AttReadBlobRsp>(
                    new AttReadBlobReq { AttributeHandle = handle, ValueOffset = (ushort)value.WrittenCount },
                    // A peer may answer that the value was not longer than the first answer
                    AttErrorCode.AttributeNotLongError,
                    request
                )
                .ConfigureAwait(false);
            if (blob is not { } next)
                break;
            part = next.PartAttributeValue;
            value.Write(part.Span);
        }
        return value.WrittenSpan[..Math.Min(value.WrittenCount, MaxValueLength)].ToArray();
    }

    private HciHostRemoteService GetService(ushort start, ushort end, BleUuid uuid)
    {
        if (!_services.TryGetValue(start, out HciHostRemoteService? service))
        {
            service = new HciHostRemoteService(this, uuid, start, end);
            _services.Add(start, service);
        }
        return service;
    }

    public HciHostRemoteCharacteristic GetCharacteristic(
        ushort valueHandle,
        ushort endHandle,
        BleUuid uuid,
        GattProperty properties
    )
    {
        lock (_lock)
        {
            if (!_characteristics.TryGetValue(valueHandle, out HciHostRemoteCharacteristic? characteristic))
            {
                characteristic = new HciHostRemoteCharacteristic(this, valueHandle, endHandle, uuid, properties);
                _characteristics.Add(valueHandle, characteristic);
            }
            return characteristic;
        }
    }

    public HciHostRemoteDescriptor GetDescriptor(ushort handle, BleUuid uuid)
    {
        if (!_descriptors.TryGetValue(handle, out HciHostRemoteDescriptor? descriptor))
        {
            descriptor = new HciHostRemoteDescriptor(this, handle, uuid);
            _descriptors.Add(handle, descriptor);
        }
        return descriptor;
    }

    [MessageSink]
    private void OnNotification(AttHandleValueNtf notification)
    {
        HciHostRemoteCharacteristic? characteristic;
        lock (_lock)
            _characteristics.TryGetValue(notification.Handle, out characteristic);
        // Notifications of characteristics nobody discovered have no reader
        if (characteristic is not null)
            _sink.OnNotification(characteristic, notification.Value.Span);
    }

    [MessageSink]
    private void OnDisconnectionComplete(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != _connection.ConnectionHandle)
            return;
        LinkDown(
            HciDisconnects.ToInfo(hciEvent.Reason),
            new BleException($"The link went down: {hciEvent.Reason}", false, (int)hciEvent.Reason, null)
        );
    }

    private void OnTransportFailed(object? sender, HciTransportFailedEventArgs args) =>
        LinkDown(new DisconnectInfo(DisconnectReason.TransportLost), args.Exception);

    /// <summary> The core learns that the link is down before anything waiting on it fails </summary>
    private void LinkDown(DisconnectInfo info, Exception reason)
    {
        if (!_down.TrySetResult(info))
            return;
        _sink.OnDisconnected(info);
        Client.End(reason);
    }

    // Requests of the peripheral to this adapter's GATT server. There are no services to serve on this side yet
    [MessageSink]
    private void OnReadByGroupTypeRequest(AttReadByGroupTypeReq<ushort> request) =>
        NoGroups(request.StartingHandle, request.EndingHandle, BleUuid.FromUInt16(request.AttributeGroupType));

    [MessageSink]
    private void OnReadByTypeRequest(AttReadByTypeReq<ushort> request) =>
        NotFound(request.OpCode, request.StartingHandle, request.EndingHandle);

    [MessageSink]
    private void OnFindByTypeValueRequest(AttFindByTypeValueReq request) =>
        NotFound(request.OpCode, request.StartingHandle, request.EndingHandle);

    [MessageSink]
    private void OnFindInformationRequest(AttFindInformationReq request) =>
        NotFound(request.OpCode, request.StartingHandle, request.EndingHandle);

    [MessageSink]
    private void OnReadRequest(AttReadReq request) =>
        Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);

    [MessageSink]
    private void OnReadBlobRequest(AttReadBlobReq request) =>
        Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);

    [MessageSink]
    private void OnWriteRequest(AttWriteReq request) =>
        Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);

    // The other requests: 128-bit type queries find nothing either, the rest is not supported. Unanswered, the peripheral
    // would run into its ATT timeout
    [MessageSink]
    private void OnUnknownPdu(L2CapPdu pdu)
    {
        if (pdu.ChannelId is not 0x0004 || pdu.Pdu.Length == 0)
            return;
        var opCode = (AttOpCode)pdu.Pdu[0];
        if (!AttServer.IsRequest(opCode))
            return;
        ReadOnlySpan<byte> body = pdu.Pdu[1..];
        // Requests with a 128-bit type
        if (opCode is AttOpCode.ATT_READ_BY_TYPE_REQ or AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ && body.Length == 20)
        {
            ushort start = BinaryPrimitives.ReadUInt16LittleEndian(body);
            ushort end = BinaryPrimitives.ReadUInt16LittleEndian(body[2..]);
            if (opCode is AttOpCode.ATT_READ_BY_TYPE_REQ)
                NotFound(opCode, start, end);
            else
                NoGroups(start, end, BleUuid.Read(body[4..]));
            return;
        }
        Error(opCode, 0x0000, AttErrorCode.RequestNotSupportedError);
    }

    /// <summary> A range starting at handle 1 that does not end before it starts holds nothing here </summary>
    private void NotFound(AttOpCode opCode, ushort start, ushort end) =>
        Error(
            opCode,
            start,
            start == 0 || end < start ? AttErrorCode.InvalidHandle : AttErrorCode.AttributeNotFoundError
        );

    /// <summary> No services in the range, if the type is one that groups at all </summary>
    private void NoGroups(ushort start, ushort end, BleUuid type)
    {
        const AttOpCode opCode = AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ;
        if (start != 0 && end >= start && !AttServer.IsGroupType(type))
            Error(opCode, start, AttErrorCode.UnsupportedGroupTypeError);
        else
            NotFound(opCode, start, end);
    }

    private void Error(AttOpCode opCode, ushort handle, AttErrorCode code) =>
        _connection.EnqueueGattErrorResponse(opCode, handle, code, null);

    /// <summary> Takes the link down unless it is down already; the core has failed this connection's operations before </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_down.Task.IsCompleted)
                await DisconnectAsync().ConfigureAwait(false);
        }
        finally
        {
            // Also when disconnecting failed: nothing answers on a released link
            Client.End(new BleException("The connection was released"));
            _device.Host.TransportFailed -= OnTransportFailed;
            _assemblerSubscription.Dispose();
            _hostSubscription.Dispose();
        }
    }

    private async Task DisconnectAsync()
    {
        await HciDisconnects.RequestAsync(_device, _connection.ConnectionHandle).ConfigureAwait(false);
        await _down
            .Task.WaitAsync(HciDisconnects.CompletionTimeout(_connection), _device.TimeProvider, CancellationToken.None)
            .ConfigureAwait(false);
    }
}
