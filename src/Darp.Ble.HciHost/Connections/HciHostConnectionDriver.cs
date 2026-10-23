using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
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
    private readonly AttClient _client;
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
        _client = new AttClient(connection);
        _hostSubscription = device.Host.Subscribe(this);
        _assemblerSubscription = connection.Assembler.Subscribe(this);
        device.Host.TransportFailed += OnTransportFailed;
    }

    public async Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request)
    {
        AttExchangeMtuRsp response = await _client
            .QueryAsync<AttExchangeMtuReq, AttExchangeMtuRsp>(
                new AttExchangeMtuReq { ClientRxMtu = (ushort)clientMtu },
                request
            )
            .ConfigureAwait(false);
        var mtu = (ushort)Math.Max(MinimumMtu, Math.Min(clientMtu, response.ServerRxMtu));
        _connection.AttMtu = mtu;
        return mtu;
    }

    public Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request) =>
        throw new NotSupportedException("Service discovery joins in a later commit");

    [MessageSink]
    private void OnDisconnectionComplete(HciDisconnectionCompleteEvent hciEvent)
    {
        if (hciEvent.ConnectionHandle != _connection.ConnectionHandle)
            return;
        LinkDown(
            ToInfo(hciEvent.Reason),
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
        _client.End(reason);
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
            _device.Host.TransportFailed -= OnTransportFailed;
            _assemblerSubscription.Dispose();
            _hostSubscription.Dispose();
        }
    }

    private async Task DisconnectAsync()
    {
        HciCommandStatus status = await _device
            .Host.QueryCommandStatusAsync(
                new HciDisconnectCommand
                {
                    ConnectionHandle = _connection.ConnectionHandle,
                    Reason = HciCommandStatus.RemoteUserTerminatedConnection,
                },
                CancellationToken.None
            )
            .ConfigureAwait(false);
        // Unknown Connection Identifier: the link went down meanwhile, and its event is on the way
        if (status is not (HciCommandStatus.Success or HciCommandStatus.UnknownConnectionIdentifier))
            throw HciCommands.Failed(status, "disconnect");
        await _down
            .Task.WaitAsync(_device.Settings.CommandTimeout, _device.TimeProvider, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static DisconnectInfo ToInfo(HciCommandStatus reason) =>
        new(
            (byte)reason switch
            {
                (byte)HciCommandStatus.ConnectionTerminatedByLocalHost => DisconnectReason.LocalRequest,
                // Supervision timeout, LL response timeout, failed to be established
                0x08 or 0x22 or 0x3E => DisconnectReason.ConnectionTimeout,
                _ => DisconnectReason.RemoteRequest,
            },
            (int)reason
        );
}
