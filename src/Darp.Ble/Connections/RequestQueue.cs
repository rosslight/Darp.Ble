using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> Hands a connection's requests to its driver one at a time, in call order, and ends the connection when the peer stops answering </summary>
/// <remarks> A request occupies the queue until the driver's task ends, even after its caller gave up </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The connection drains the queue once it ended; draining releases the timer and the token source"
)]
internal sealed class RequestQueue
{
    /// <summary> How long a peer may take to answer a transaction (Core spec Vol 3 Part F 3.3.3) </summary>
    private static readonly TimeSpan AttTimeout = TimeSpan.FromSeconds(30);

    private readonly BleAddress _address;
    private readonly TimeProvider _timeProvider;
    private readonly Action<DisconnectInfo> _end;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _closed = new();
    private readonly ITimer _timer;
    private Task _tail = Task.CompletedTask;
    private readonly TaskCompletionSource _handOffsEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DisconnectInfo? _closedInfo;
    private int _handingOff;

    // When the transaction waiting for its answer was sent, as a monotonic timestamp of the time provider
    private long? _sentAt;

    /// <summary> Creates the queue of one connection </summary>
    /// <param name="address"> The peer, for logging </param>
    /// <param name="timeProvider"> The clock of the ATT timeout </param>
    /// <param name="end"> Ends the connection, when the peer stops answering </param>
    /// <param name="logger"> The logger </param>
    public RequestQueue(BleAddress address, TimeProvider timeProvider, Action<DisconnectInfo> end, ILogger logger)
    {
        _address = address;
        _timeProvider = timeProvider;
        _end = end;
        _logger = logger;
        _timer = timeProvider.CreateTimer(
            _ => OnTimer(),
            state: null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan
        );
    }

    /// <summary> Runs a request once every earlier one ended </summary>
    /// <param name="send"> Sends the request through the driver </param>
    /// <param name="cancellationToken"> Gives up waiting. A request still queued is dropped; a sent one ends abandoned </param>
    /// <exception cref="BleDisconnectedException"> The connection ended before or while the request ran </exception>
    public async Task<T> RunAsync<T>(Func<GattRequest, Task<T>> send, CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task previous;
        lock (_lock)
        {
            if (_closedInfo is not null)
                throw new BleDisconnectedException(_closedInfo);
            previous = _tail;
            _tail = done.Task;
        }
        if (!await WaitForTurnAsync(previous, cancellationToken).ConfigureAwait(false))
        {
            // The request never reaches the driver; the next one still waits for the earlier ones
            _ = previous.ContinueWith(_ => done.TrySetResult(), TaskScheduler.Default);
            cancellationToken.ThrowIfCancellationRequested();
            throw Disconnected(innerException: null);
        }
        bool admitted;
        lock (_lock)
        {
            // Teardown waits for admitted hand-offs, so the driver is not released before it got the request
            admitted = _closedInfo is null && !cancellationToken.IsCancellationRequested;
            if (admitted)
                _handingOff++;
        }
        if (!admitted)
        {
            done.TrySetResult();
            cancellationToken.ThrowIfCancellationRequested();
            throw Disconnected(innerException: null);
        }

        return await SendAsync(send, done, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Hands an admitted request to the driver and waits for its answer or the caller's cancellation </summary>
    private async Task<T> SendAsync<T>(
        Func<GattRequest, Task<T>> send,
        TaskCompletionSource done,
        CancellationToken cancellationToken
    )
    {
        var request = new GattRequest(this, _timeProvider.GetTimestamp(), _closed.Token);
        // Cancels the hand-off as soon as the caller gives up, also while the driver still runs synchronously
        CancellationTokenRegistration registration = cancellationToken.Register(() => request.Cancel());
        Task<T> answer;
        try
        {
            answer = InvokeAsync(send, request);
        }
        finally
        {
            EndHandOff();
        }
        _ = answer.ContinueWith(
            _ =>
            {
                registration.Dispose();
                Complete(request, done);
            },
            TaskScheduler.Default
        );
        T result;
        try
        {
            result = await answer.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (request.Cancel())
                _logger.LogRequestAbandoned(_address);
            throw;
        }
        catch (Exception e) when (IsClosed)
        {
            // The driver fails what it ran when the link goes; the caller learns why the connection ended
            throw Disconnected(e);
        }
        // An answer that completes once the connection ended does not count; the caller sees the same end as Disconnected
        if (IsClosed)
            throw Disconnected(innerException: null);
        return result;
    }

    /// <summary> Fails queued and new requests with the end of the connection; running ones end with the driver </summary>
    public void Close(DisconnectInfo info)
    {
        lock (_lock)
        {
            if (_closedInfo is not null)
                return;
            _closedInfo = info;
            _sentAt = null;
            if (_handingOff == 0)
                _handOffsEnded.TrySetResult();
        }
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _closed.Cancel();
    }

    /// <summary> Completes once every request admitted before <see cref="Close"/> reached the driver; then the driver may be released </summary>
    public Task WhenHandOffsEnded => _handOffsEnded.Task;

    /// <summary> Waits until every request ended, then releases the timer. Call after <see cref="Close"/> and once the driver ended its tasks </summary>
    public async Task DrainAsync()
    {
        Task tail;
        lock (_lock)
            tail = _tail;
        await tail.ConfigureAwait(false);
        await _timer.DisposeAsync().ConfigureAwait(false);
        _closed.Dispose();
    }

    internal void OnTransactionSent()
    {
        lock (_lock)
        {
            if (_closedInfo is not null)
                return;
            _sentAt = _timeProvider.GetTimestamp();
        }
        _timer.Change(AttTimeout, Timeout.InfiniteTimeSpan);
    }

    internal void OnTransactionAnswered()
    {
        lock (_lock)
            _sentAt = null;
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private bool IsClosed
    {
        get
        {
            lock (_lock)
                return _closedInfo is not null;
        }
    }

    private async Task<bool> WaitForTurnAsync(Task previous, CancellationToken cancellationToken)
    {
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closed.Token);
        try
        {
            await previous.WaitAsync(wait.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        return !IsClosed;
    }

    private static async Task<T> InvokeAsync<T>(Func<GattRequest, Task<T>> send, GattRequest request) =>
        await send(request).ConfigureAwait(false);

    private void EndHandOff()
    {
        lock (_lock)
        {
            _handingOff--;
            if (_handingOff == 0 && _closedInfo is not null)
                _handOffsEnded.TrySetResult();
        }
    }

    private void Complete(GattRequest request, TaskCompletionSource done)
    {
        request.Complete();
        if (request.IsAbandoned && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogAbandonedRequestCompleted(_address, _timeProvider.GetElapsedTime(request.StartedAt));
        done.TrySetResult();
    }

    private void OnTimer()
    {
        lock (_lock)
        {
            if (_closedInfo is not null || _sentAt is not { } sentAt)
                return;
            // A timer armed for an earlier transaction may fire before the one waiting now is due
            TimeSpan remaining = AttTimeout - _timeProvider.GetElapsedTime(sentAt);
            if (remaining > TimeSpan.Zero)
            {
                _timer.Change(remaining, Timeout.InfiniteTimeSpan);
                return;
            }
            _sentAt = null;
        }
        _logger.LogAttTimeout(_address);
        _end(new DisconnectInfo(DisconnectReason.AttTimeout));
    }

    private BleDisconnectedException Disconnected(Exception? innerException)
    {
        DisconnectInfo info;
        lock (_lock)
            info = _closedInfo ?? throw new InvalidOperationException("The queue is not closed");
        return new BleDisconnectedException(info, innerException);
    }
}
