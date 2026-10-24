using Darp.Ble.Backends;
using Darp.Ble.Diagnostics;
using Darp.Ble.Gatt;

namespace Darp.Ble.Connections;

/// <summary> The notifications of one characteristic on one connection: whether they are on, and the queues of its streams (D7) </summary>
/// <remarks> The connection owns it; the same channel serves every wrapper of the characteristic </remarks>
internal sealed class NotificationChannel
{
    private readonly IRemoteCharacteristicDriver _driver;
    private readonly string _backend;
    private readonly Lock _lock = new();
    private ItemQueue<byte[]>[] _queues = [];
    private bool _isOn;
    private int _enabling;
    private Exception? _ended;
    private bool _endedNormally;

    public NotificationChannel(IRemoteCharacteristicDriver driver, string backend)
    {
        _driver = driver;
        _backend = backend;
    }

    /// <summary> Registers the queue of a new stream; a stream of an ended connection ends at once the same way </summary>
    public NotificationStream<T> Subscribe<T>(QueueOptions queue, Func<byte[], T> decode)
    {
        var items = new ItemQueue<byte[]>(queue, onDrop: CountDrop);
        bool ended;
        lock (_lock)
        {
            ended = _ended is not null || _endedNormally;
            if (!ended)
                _queues = [.. _queues, items];
        }
        if (ended)
            items.Complete(_ended);
        return new NotificationStream<T>(this, items, decode);
    }

    /// <summary> Runs on the driver's thread; allocation-free with the fixed tags </summary>
    private void CountDrop() =>
        Telemetry.Dropped.Add(
            1,
            new System.Diagnostics.TagList
            {
                { Telemetry.BackendTag, _backend },
                { Telemetry.QueueTag, "notifications" },
            }
        );

    public void Remove(ItemQueue<byte[]> queue)
    {
        lock (_lock)
            _queues = Without(_queues, queue);
    }

    /// <summary> Throws unless notifications are on or being enabled; a stream that already ended can always be read to its end </summary>
    public void EnsureCanListen(ItemQueue<byte[]> queue)
    {
        lock (_lock)
        {
            if (_isOn || _enabling > 0 || queue.IsCompleted)
                return;
        }
        throw new InvalidOperationException(
            "Notifications are not enabled; call EnableNotificationsAsync before enumerating the stream"
        );
    }

    /// <summary> A notification arrived; every stream gets its own copy. Called on the driver's thread </summary>
    public void Deliver(ReadOnlySpan<byte> value)
    {
        ItemQueue<byte[]>[] queues;
        lock (_lock)
            queues = _queues;
        foreach (ItemQueue<byte[]> queue in queues)
        {
            if (!queue.TryWrite(value.ToArray()))
                Remove(queue);
        }
    }

    /// <summary> Writes the CCCD to turn notifications on, unless they are on when the request's turn comes </summary>
    public Task EnableAsync(ConnectedPeripheral connection, CancellationToken cancellationToken) =>
        SetAsync(connection, enabled: true, cancellationToken);

    /// <summary> Writes the CCCD to turn notifications off; every open stream ends </summary>
    public Task DisableAsync(ConnectedPeripheral connection, CancellationToken cancellationToken) =>
        SetAsync(connection, enabled: false, cancellationToken);

    /// <summary> The connection ended: streams end normally after a local end, with the disconnection otherwise </summary>
    public void End(DisconnectInfo info)
    {
        Exception? error = info.Reason is DisconnectReason.LocalRequest ? null : new BleDisconnectedException(info);
        ItemQueue<byte[]>[] queues;
        lock (_lock)
        {
            _ended = error;
            _endedNormally = error is null;
            queues = _queues;
            _queues = [];
        }
        foreach (ItemQueue<byte[]> queue in queues)
            queue.Complete(error);
    }

    private async Task SetAsync(ConnectedPeripheral connection, bool enabled, CancellationToken cancellationToken)
    {
        var attempt = new Attempt();
        if (enabled)
        {
            lock (_lock)
                _enabling++;
        }
        try
        {
            await connection
                .RunAsync(
                    GattOperation.SetNotifications(_driver.Uuid, enabled),
                    request => WriteCccdAsync(enabled, attempt, request),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            // A request that never reached the driver still has to give up its part in enabling
            if (enabled && !attempt.Started)
                EndEnabling(error: null);
        }
    }

    private async Task WriteCccdAsync(bool enabled, Attempt attempt, GattRequest request)
    {
        attempt.Started = true;
        Exception? error = null;
        try
        {
            bool unchanged;
            lock (_lock)
                unchanged = _isOn == enabled;
            if (!unchanged)
                await _driver.SetNotificationsAsync(enabled, request).ConfigureAwait(false);
            // The device's actual state, also when the caller gave up meanwhile
            lock (_lock)
                _isOn = enabled;
        }
        catch (Exception e)
        {
            error = e;
            throw;
        }
        finally
        {
            if (enabled)
                EndEnabling(error);
            else if (error is null)
                EndStreams(new InvalidOperationException("Notifications were disabled"));
        }
    }

    /// <summary> One enable ended; when notifications ended up off, the streams can never get anything and end </summary>
    private void EndEnabling(Exception? error)
    {
        bool off;
        lock (_lock)
        {
            _enabling--;
            off = !_isOn && _enabling == 0;
        }
        if (off)
            EndStreams(new InvalidOperationException("Enabling notifications failed", error));
    }

    private void EndStreams(Exception error)
    {
        ItemQueue<byte[]>[] queues;
        lock (_lock)
        {
            queues = _queues;
            _queues = [];
        }
        foreach (ItemQueue<byte[]> queue in queues)
            queue.Complete(error);
    }

    private static ItemQueue<byte[]>[] Without(ItemQueue<byte[]>[] queues, ItemQueue<byte[]> queue) =>
        Array.IndexOf(queues, queue) < 0 ? queues : [.. queues.Where(q => !ReferenceEquals(q, queue))];

    /// <summary> Whether a CCCD request reached the driver </summary>
    private sealed class Attempt
    {
        public bool Started { get; set; }
    }
}
