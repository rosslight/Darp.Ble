using Darp.Ble.Connections;

namespace Darp.Ble.Gatt.Services;

/// <summary> The echo service of a connected peripheral </summary>
/// <remarks> The one request/response helper of the library: echo replies carry no correlation, so queries run one at a time </remarks>
public sealed class RemoteEchoService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly TimeProvider _timeProvider;
    private readonly EchoSession _session;

    internal RemoteEchoService(
        RemoteCharacteristic<Properties.Write> write,
        RemoteCharacteristic<Properties.Notify> notify,
        TimeProvider timeProvider
    )
    {
        Write = write;
        Notify = notify;
        _timeProvider = timeProvider;
        RemoteGattCharacteristic replies = notify.Characteristic;
        // Every wrapper of this service on this connection shares one session
        _session = replies.Connection.GetOrAddHelper(replies.Driver, () => new EchoSession());
    }

    /// <summary> The characteristic requests are written to </summary>
    public RemoteCharacteristic<Properties.Write> Write { get; }

    /// <summary> The characteristic replies are notified on </summary>
    public RemoteCharacteristic<Properties.Notify> Notify { get; }

    /// <summary> Writes a request and returns the next notification as its reply </summary>
    /// <remarks> Subscribes before writing, so an immediate reply is not lost. Turns notifications on and leaves them on.
    /// Queries of one service on one connection run one after another. Replies carry no correlation, so once a query started
    /// its write and then failed (timeout, cancellation, error), even before anything was sent, a late reply could be taken
    /// for the next one: later queries throw <see cref="InvalidOperationException"/> until the app reconnects </remarks>
    /// <param name="request"> The request </param>
    /// <param name="timeout"> How long to wait for the write and the reply together; 10 s when null </param>
    /// <param name="cancellationToken"> Cancels the query </param>
    /// <returns> The reply </returns>
    /// <exception cref="TimeoutException"> No reply within the timeout </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    /// <exception cref="InvalidOperationException"> An earlier query on this connection got no reply </exception>
    public async Task<byte[]> QueryOneAsync(
        ReadOnlyMemory<byte> request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        TimeSpan budget = timeout ?? DefaultTimeout;
        using var deadline = new CancellationTokenSource(budget, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            await _session.Queries.WaitAsync(linked.Token).ConfigureAwait(false);
            var writing = false;
            try
            {
                if (_session.IsBroken)
                    throw new InvalidOperationException(
                        "An earlier echo query got no reply; reconnect before querying again"
                    );
                using NotificationStream<byte[]> replies = Notify.Subscribe();
                await Notify.EnableNotificationsAsync(linked.Token).ConfigureAwait(false);
                writing = true;
                await Write.WriteAsync(request, linked.Token).ConfigureAwait(false);
                byte[] reply = await replies.FirstAsync(linked.Token).ConfigureAwait(false);
                writing = false;
                return reply;
            }
            catch when (writing)
            {
                // The request may have gone out; its reply may still come
                _session.Break();
                throw;
            }
            finally
            {
                _session.Queries.Release();
            }
        }
        catch (OperationCanceledException e)
            when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No reply within {budget}", e);
        }
    }
}
