using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Connections;

/// <summary> Waits for the connection complete event of this adapter's attempt to connect to one peripheral as a central </summary>
internal sealed partial class ConnectAttempt : IDisposable
{
    private readonly HciDevice _device;
    private readonly IConnectionSink _sink;
    private readonly BleAddress _peer;
    private readonly IDisposable _subscription;
    private readonly TaskCompletionSource<HciHostConnectionDriver> _result = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    /// <summary> Listens from now on, so a fast connection complete event cannot be missed </summary>
    public ConnectAttempt(HciDevice device, IConnectionSink sink, BleAddress peer)
    {
        _device = device;
        _sink = sink;
        _peer = peer;
        _subscription = device.Host.Subscribe(this);
        device.Host.TransportFailed += OnTransportFailed;
    }

    public Task<HciHostConnectionDriver> Task => _result.Task;

    private void OnTransportFailed(object? sender, HciTransportFailedEventArgs args) =>
        _result.TrySetException(
            new BleConnectException(
                BleConnectErrorKind.Unreachable,
                "The HCI transport failed while connecting",
                innerException: args.Exception
            )
        );

    public void Dispose()
    {
        _device.Host.TransportFailed -= OnTransportFailed;
        _subscription.Dispose();
    }

    [MessageSink]
    private void OnConnectionComplete(HciLeEnhancedConnectionCompleteV1Event hciEvent)
    {
        if (hciEvent.Role is not HciLeConnectionRole.Central || _result.Task.IsCompleted)
            return;
        if (hciEvent.Status is HciCommandStatus.UnknownConnectionIdentifier)
        {
            // The attempt was cancelled
            _result.TrySetCanceled();
            return;
        }
        if (hciEvent.Status is not HciCommandStatus.Success)
        {
            _result.TrySetException(
                new BleConnectException(
                    BleConnectErrorKind.Unreachable,
                    $"The controller could not connect: {hciEvent.Status}",
                    (int)hciEvent.Status
                )
            );
            return;
        }
        // A link to another peer is not this attempt's
        if (hciEvent.PeerAddress.ToUInt64() != _peer.Value.ToUInt64())
            return;
        if (!_device.TryGetConnection(hciEvent.ConnectionHandle, out AclConnection? connection))
            return;
#pragma warning disable CA2000 // The caller owns the driver; a cancelled caller disposes it
        _result.TrySetResult(new HciHostConnectionDriver(_device, connection, _sink));
#pragma warning restore CA2000
    }
}
