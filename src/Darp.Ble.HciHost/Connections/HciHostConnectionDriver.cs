using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
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

    // One object per attribute, so every discovery returns the same ones
    private readonly Dictionary<ushort, HciHostRemoteService> _services = [];
    private readonly Dictionary<ushort, HciHostRemoteCharacteristic> _characteristics = [];
    private readonly Dictionary<ushort, HciHostRemoteDescriptor> _descriptors = [];
    private readonly IDisposable _hostSubscription;
    private readonly IDisposable _assemblerSubscription;
    private const int MinimumMtu = 23;

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
        AttExchangeMtuRsp response = await Client
            .QueryAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
                new AttExchangeMtuReq { ClientRxMtu = (ushort)clientMtu },
                request
            )
            .ConfigureAwait(false);
        var mtu = (ushort)Math.Max(MinimumMtu, Math.Min(clientMtu, response.ServerRxMtu));
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
                .QueryUnlessNotFoundAsync<AttReadByGroupTypeReq<ushort>, AttReadByGroupTypeRsp>(
                    new AttReadByGroupTypeReq<ushort>
                    {
                        StartingHandle = start,
                        EndingHandle = 0xFFFF,
                        AttributeGroupType = 0x2800,
                    },
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

    public Task<byte[]> ReadAsync(ushort handle, GattRequest request) => ReadCoreAsync(handle, request);

    public Task WriteAsync(ushort handle, ReadOnlyMemory<byte> value, GattRequest request)
    {
        CheckLength(value);
        return Client.QueryAsync<AttWriteReq, AttWriteRsp>(
            new AttWriteReq { AttributeHandle = handle, AttributeValue = value },
            request
        );
    }

    /// <summary> Sends a write command, which the peer does not answer </summary>
    public ValueTask WriteCommandAsync(ushort handle, ReadOnlyMemory<byte> value, GattRequest request)
    {
        CheckLength(value);
        // Completes once the controller has a buffer for the command, which paces a loop of writes (D8)
        return _connection.SendGattPacketAsync(
            new AttWriteCmd { Handle = handle, Value = value },
            onHandOff: null,
            request.HandOffToken,
            request.ConnectionClosedToken
        );
    }

    private async Task<byte[]> ReadCoreAsync(ushort handle, GattRequest request)
    {
        AttReadRsp response = await Client
            .QueryAsync<AttReadReq, AttReadRsp>(new AttReadReq { AttributeHandle = handle }, request)
            .ConfigureAwait(false);
        return response.AttributeValue.ToArray();
    }

    /// <summary> Long writes would need prepared writes, which v2 leaves out (D8) </summary>
    private void CheckLength(ReadOnlyMemory<byte> value)
    {
        int max = _connection.AttMtu - 3;
        if (value.Length > max)
            throw new ArgumentOutOfRangeException(nameof(value), value.Length, $"The MTU allows at most {max} bytes");
    }

    private HciHostRemoteService GetService(ushort start, ushort end, BleUuid uuid)
    {
        lock (_lock)
        {
            if (!_services.TryGetValue(start, out HciHostRemoteService? service))
            {
                service = new HciHostRemoteService(this, uuid, start, end);
                _services.Add(start, service);
            }
            return service;
        }
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
        lock (_lock)
        {
            if (!_descriptors.TryGetValue(handle, out HciHostRemoteDescriptor? descriptor))
            {
                descriptor = new HciHostRemoteDescriptor(this, handle, uuid);
                _descriptors.Add(handle, descriptor);
            }
            return descriptor;
        }
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
        NotFound(request.OpCode, request.StartingHandle);

    [MessageSink]
    private void OnReadByTypeRequest(AttReadByTypeReq<ushort> request) =>
        NotFound(request.OpCode, request.StartingHandle);

    [MessageSink]
    private void OnFindByTypeValueRequest(AttFindByTypeValueReq request) =>
        NotFound(request.OpCode, request.StartingHandle);

    [MessageSink]
    private void OnFindInformationRequest(AttFindInformationReq request) =>
        NotFound(request.OpCode, request.StartingHandle);

    [MessageSink]
    private void OnReadRequest(AttReadReq request) =>
        _connection.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle, null);

    [MessageSink]
    private void OnWriteRequest(AttWriteReq request) =>
        _connection.EnqueueGattErrorResponse(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle, null);

    private void NotFound(AttOpCode opCode, ushort startingHandle) =>
        _connection.EnqueueGattErrorResponse(opCode, startingHandle, AttErrorCode.AttributeNotFoundError, null);

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
            .Task.WaitAsync(_device.Settings.CommandTimeout, _device.TimeProvider, CancellationToken.None)
            .ConfigureAwait(false);
    }
}
