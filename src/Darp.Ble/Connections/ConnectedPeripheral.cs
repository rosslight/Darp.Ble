using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> A connection to a peripheral, from the central side. It ends once; afterwards it cannot be used again </summary>
public sealed class ConnectedPeripheral : IAsyncDisposable
{
    /// <summary> The largest ATT MTU the core uses: the longest attribute value (512) plus a write's header (5) </summary>
    private const int ClientMtu = 517;

    /// <summary> The header of a write request: the opcode and the attribute handle </summary>
    private const int WriteHeaderLength = 3;

    private readonly BleCentral _central;
    private readonly IConnectionDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource<DisconnectInfo> _disconnected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly RequestQueue _requests;
    private readonly Dictionary<IRemoteCharacteristicDriver, NotificationChannel> _notifications = [];
    private DisconnectInfo? _ending;
    private int _maxWritePayloadLength;

    internal ConnectedPeripheral(
        BleCentral central,
        BleAddress address,
        IConnectionDriver driver,
        TimeProvider timeProvider,
        QueueOptions notificationQueue,
        ILogger logger
    )
    {
        NotificationQueue = notificationQueue;
        _central = central;
        _driver = driver;
        _logger = logger;
        _requests = new RequestQueue(address, timeProvider, End, logger);
        Address = address;
        Gatt = new GattClient(this, driver);
    }

    /// <summary> The address of the peripheral </summary>
    public BleAddress Address { get; }

    /// <summary> The services of the peripheral </summary>
    public GattClient Gatt { get; }

    /// <summary> The longest value a write can carry, from the MTU agreed when connecting </summary>
    public int MaxWritePayloadLength => _maxWritePayloadLength;

    /// <summary> Completes once, when the connection ended, with why it ended </summary>
    public Task<DisconnectInfo> Disconnected => _disconnected.Task;

    /// <summary> Ends the connection. Calling it again, or after the peripheral disconnected, waits for the same end </summary>
    /// <param name="cancellationToken"> Stops waiting; the connection still ends </param>
    /// <returns> A task that completes once the connection ended </returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        End(new DisconnectInfo(DisconnectReason.LocalRequest));
        await _disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Ends the connection; the same as <see cref="DisconnectAsync"/> </summary>
    /// <returns> A task that completes once the connection ended </returns>
    public ValueTask DisposeAsync() => new(DisconnectAsync(CancellationToken.None));

    /// <summary> Runs a request of this connection once the earlier ones ended </summary>
    internal Task<T> RunAsync<T>(Func<GattRequest, Task<T>> send, CancellationToken cancellationToken) =>
        _requests.RunAsync(send, cancellationToken);

    /// <summary> Runs a write without response in this connection's send order </summary>
    internal ValueTask RunCommandAsync(
        IRemoteCharacteristicDriver characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) => _requests.RunCommandAsync(characteristic, value, cancellationToken);

    /// <summary> Runs a request without a result of this connection once the earlier ones ended </summary>
    internal Task RunAsync(Func<GattRequest, Task> send, CancellationToken cancellationToken) =>
        _requests.RunAsync(
            async request =>
            {
                await send(request).ConfigureAwait(false);
                return true;
            },
            cancellationToken
        );

    /// <summary> The queue of a stream that does not bring its own </summary>
    internal QueueOptions NotificationQueue { get; }

    /// <summary> The notifications of a characteristic; the same channel for the same driver object </summary>
    internal NotificationChannel GetNotifications(IRemoteCharacteristicDriver characteristic)
    {
        NotificationChannel? channel;
        DisconnectInfo? ending;
        lock (_lock)
        {
            ending = _ending;
            if (!_notifications.TryGetValue(characteristic, out channel))
            {
                channel = new NotificationChannel(characteristic);
                _notifications.Add(characteristic, channel);
            }
        }
        // A channel created after the end ends at once, so its streams end like all others
        if (ending is not null)
            channel.End(ending);
        return channel;
    }

    internal void OnNotification(IRemoteCharacteristicDriver characteristic, ReadOnlySpan<byte> value)
    {
        NotificationChannel? channel;
        lock (_lock)
            _notifications.TryGetValue(characteristic, out channel);
        // Without a channel nobody subscribed: dropped
        channel?.Deliver(value);
    }

    /// <summary> Why the connection ends, once it started ending </summary>
    internal DisconnectInfo? Ending
    {
        get
        {
            lock (_lock)
                return _ending;
        }
    }

    /// <summary> Agrees on the MTU, as the connection's first request; before the connection is handed out </summary>
    internal async Task ExchangeMtuAsync(CancellationToken cancellationToken)
    {
        int mtu = await _requests
            .RunAsync(request => _driver.ExchangeMtuAsync(ClientMtu, request), cancellationToken)
            .ConfigureAwait(false);
        _maxWritePayloadLength = mtu - WriteHeaderLength;
    }

    /// <summary> Ends the connection exactly once; the first reason wins </summary>
    internal void End(DisconnectInfo info)
    {
        lock (_lock)
        {
            if (_ending is not null)
                return;
            _ending = info;
        }
        // Requests fail with this end from now on; the ones running end with the driver
        _requests.Close(info);
        // Streams deliver what they queued, then end the same way (D7)
        NotificationChannel[] channels;
        lock (_lock)
            channels = [.. _notifications.Values];
        foreach (NotificationChannel channel in channels)
            channel.End(info);
        // Off the caller's thread: a driver may report a disconnect from its read loop, and teardown calls back into it
        _ = Task.Run(() => TeardownAsync(info));
    }

    private async Task TeardownAsync(DisconnectInfo info)
    {
        try
        {
            await _requests.WhenHandOffsEnded.ConfigureAwait(false);
            await _driver.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The connection is over either way; whoever waits learns it through Disconnected
            _logger.LogConnectionCleanupFailed(Address, e);
        }
        finally
        {
            await _requests.DrainAsync().ConfigureAwait(false);
            _central.Remove(this);
            _logger.LogDisconnected(Address, info.Reason);
            _disconnected.TrySetResult(info);
        }
    }
}
