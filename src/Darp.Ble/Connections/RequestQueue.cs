using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Connections;

/// <summary> Hands a connection's operations to its driver in call order and ends the connection when the peer stops answering (D11) </summary>
/// <remarks>
/// Two things are separate: the send order, which every operation keeps, and the one ATT slot, which only requests take.
/// An operation leaves the send order once it is on its way: a request at its first transaction, a write without response once
/// the backend accepted it. A request keeps the slot until the driver's task ends, also after its caller gave up, so a write
/// without response may follow a sent request but never overtakes a request still waiting for the slot
/// </remarks>
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
    private readonly TaskCompletionSource _handOffsEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Holds the send order for a write without response that has no turn of its own
    private static readonly object FastCommand = new();

    // Operations waiting for their turn, in call order
    private readonly LinkedList<Turn> _waiting = new();

    // The operation handed over that has not left the send order yet; the shared command request on the fast path
    private object? _sending;

    // The request that holds the ATT slot
    private Turn? _slotOwner;

    // Driver calls in progress, and operations admitted but not finished
    private int _invoking;
    private int _unfinished;
    private DisconnectInfo? _closedInfo;

    // The transaction waiting for its answer, and when it was sent, as a monotonic timestamp of the time provider
    private GattRequest? _transaction;
    private long _sentAt;

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

    /// <summary> Completes once every operation admitted before <see cref="Close"/> reached the driver; then the driver may be released </summary>
    public Task WhenHandOffsEnded => _handOffsEnded.Task;

    private bool IsClosed
    {
        get
        {
            lock (_lock)
                return _closedInfo is not null;
        }
    }

    /// <summary> Runs a request, which takes the ATT slot until the driver's task ends </summary>
    /// <param name="send"> Sends the request through the driver </param>
    /// <param name="cancellationToken"> Gives up waiting. A request still queued is dropped; a sent one ends abandoned </param>
    /// <exception cref="BleDisconnectedException"> The connection ended before or while the request ran </exception>
    public async Task<T> RunAsync<T>(Func<GattRequest, Task<T>> send, CancellationToken cancellationToken)
    {
        var turn = new Turn(isRequest: true);
        await WaitForTurnAsync(turn, cancellationToken).ConfigureAwait(false);
        var request = GattRequest.ForRequest(this, turn, _timeProvider.GetTimestamp(), _closed.Token);
        // Cancels the hand-off as soon as the caller gives up, also while the driver still runs synchronously
        CancellationTokenRegistration registration = cancellationToken.Register(() => request.Cancel());
        Task<T> answer;
        try
        {
            answer = InvokeAsync(send, request);
        }
        finally
        {
            EndInvocation();
        }
        _ = answer.ContinueWith(
            _ =>
            {
                registration.Dispose();
                Complete(request, turn);
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

    /// <summary> Runs a write without response, which leaves the send order once the backend accepted it (D8) </summary>
    /// <remarks> With nothing queued and a driver that accepts at once, this allocates nothing </remarks>
    /// <exception cref="BleDisconnectedException"> The connection ended before the backend accepted the write </exception>
    public ValueTask RunCommandAsync(
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        bool fast;
        lock (_lock)
        {
            if (_closedInfo is not null)
                return ValueTask.FromException(new BleDisconnectedException(_closedInfo));
            fast = _waiting.Count == 0 && _sending is null && !cancellationToken.IsCancellationRequested;
            if (fast)
            {
                _sending = FastCommand;
                _invoking++;
                _unfinished++;
            }
        }
        return fast
            ? InvokeCommand(driver, value, turn: null, cancellationToken)
            : QueueCommandAsync(driver, value, cancellationToken);
    }

    /// <summary> Fails queued and new operations with the end of the connection; running ones end with the driver. Called once </summary>
    public void Close(DisconnectInfo info)
    {
        Turn[] dropped;
        lock (_lock)
        {
            _closedInfo = info;
            _transaction = null;
            dropped = [.. _waiting];
            _waiting.Clear();
            foreach (Turn turn in dropped)
                turn.State = TurnState.Dropped;
            _unfinished -= dropped.Length;
            if (_invoking == 0)
                _handOffsEnded.TrySetResult();
            if (_unfinished == 0)
                _drained.TrySetResult();
        }
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _closed.Cancel();
        foreach (Turn turn in dropped)
            turn.Granted.TrySetException(new BleDisconnectedException(info));
    }

    /// <summary> Waits until every operation ended, then releases the timer. Call after <see cref="Close"/> and once the driver ended its tasks </summary>
    public async Task DrainAsync()
    {
        await _drained.Task.ConfigureAwait(false);
        await _timer.DisposeAsync().ConfigureAwait(false);
        _closed.Dispose();
    }

    internal void OnTransactionSent(GattRequest request)
    {
        bool leftSendOrder;
        lock (_lock)
        {
            if (_closedInfo is not null)
                return;
            _transaction = request;
            _sentAt = _timeProvider.GetTimestamp();
            // The first transaction puts the request on its way; the next operation may follow
            leftSendOrder = ReferenceEquals(_sending, request.Turn);
            if (leftSendOrder)
                _sending = null;
            _timer.Change(AttTimeout, Timeout.InfiniteTimeSpan);
        }
        if (leftSendOrder)
            Dispatch();
    }

    internal void OnTransactionAnswered(GattRequest request)
    {
        lock (_lock)
        {
            if (!ReferenceEquals(_transaction, request))
                return;
            _transaction = null;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task WaitForTurnAsync(Turn turn, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_closedInfo is not null)
                throw new BleDisconnectedException(_closedInfo);
            _waiting.AddLast(turn);
            _unfinished++;
        }
        Dispatch();
        try
        {
            await turn.Granted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Withdraw(turn);
            throw;
        }
        bool admitted;
        lock (_lock)
        {
            // Teardown waits for admitted hand-offs, so the driver is not released before it got the operation
            admitted = _closedInfo is null && !cancellationToken.IsCancellationRequested;
            if (admitted)
                _invoking++;
        }
        if (admitted)
            return;
        Finish(turn);
        cancellationToken.ThrowIfCancellationRequested();
        throw Disconnected(innerException: null);
    }

    private async ValueTask QueueCommandAsync(
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        var turn = new Turn(isRequest: false);
        await WaitForTurnAsync(turn, cancellationToken).ConfigureAwait(false);
        await InvokeCommand(driver, value, turn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Hands a write without response to the driver; the operation holds the send order until the backend accepted it </summary>
    private ValueTask InvokeCommand(
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        Turn? turn,
        CancellationToken cancellationToken
    )
    {
        ValueTask accepted;
        try
        {
            accepted = driver.WriteWithoutResponseAsync(value, cancellationToken, _closed.Token);
        }
        catch (Exception e)
        {
            FinishCommand(turn);
            return ValueTask.FromException(Translate(e, cancellationToken));
        }
        finally
        {
            EndInvocation();
        }
        if (!accepted.IsCompletedSuccessfully)
            return AwaitCommandAsync(accepted, turn, cancellationToken);
        // Consumes the result, so a pooled source behind the task can be reused; it completed, so this never blocks
#pragma warning disable CA1849
        accepted.GetAwaiter().GetResult();
#pragma warning restore CA1849
        FinishCommand(turn);
        return IsClosed ? ValueTask.FromException(Disconnected(innerException: null)) : ValueTask.CompletedTask;
    }

    private async ValueTask AwaitCommandAsync(ValueTask accepted, Turn? turn, CancellationToken cancellationToken)
    {
        try
        {
            await accepted.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw Translate(e, cancellationToken);
        }
        finally
        {
            FinishCommand(turn);
        }
        if (IsClosed)
            throw Disconnected(innerException: null);
    }

    private Exception Translate(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested ? exception
        : IsClosed ? Disconnected(exception)
        : exception;

    private void FinishCommand(Turn? turn)
    {
        if (turn is not null)
        {
            Finish(turn);
            return;
        }
        lock (_lock)
        {
            if (ReferenceEquals(_sending, FastCommand))
                _sending = null;
            OperationEnded();
        }
        Dispatch();
    }

    /// <summary> Gives the next operation its turn, if the send order is free and, for a request, the ATT slot too </summary>
    private void Dispatch()
    {
        Turn granted;
        lock (_lock)
        {
            if (_closedInfo is not null || _sending is not null || _waiting.First is not { } head)
                return;
            if (head.Value.IsRequest && _slotOwner is not null)
                return;
            _waiting.RemoveFirst();
            granted = head.Value;
            granted.State = TurnState.Granted;
            _sending = granted;
            if (granted.IsRequest)
                _slotOwner = granted;
        }
        granted.Granted.TrySetResult();
    }

    /// <summary> A caller gave up while its operation waited; it may already have got its turn </summary>
    private void Withdraw(Turn turn)
    {
        TurnState state;
        lock (_lock)
        {
            state = turn.State;
            if (state is TurnState.Waiting)
            {
                _waiting.Remove(turn);
                turn.State = TurnState.Dropped;
                OperationEnded();
            }
        }
        switch (state)
        {
            case TurnState.Granted:
                Finish(turn);
                break;
            case TurnState.Waiting:
                // The withdrawn turn may have held up the operations behind it
                Dispatch();
                break;
            case TurnState.Dropped:
                // Close already accounted for it
                break;
        }
    }

    /// <summary> An operation ended: it leaves the send order and gives up the ATT slot </summary>
    private void Finish(Turn turn)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_sending, turn))
                _sending = null;
            if (ReferenceEquals(_slotOwner, turn))
                _slotOwner = null;
            OperationEnded();
        }
        Dispatch();
    }

    /// <summary> Called under <see cref="_lock"/> </summary>
    private void OperationEnded()
    {
        _unfinished--;
        if (_closedInfo is not null && _unfinished == 0)
            _drained.TrySetResult();
    }

    private void EndInvocation()
    {
        lock (_lock)
        {
            _invoking--;
            if (_invoking == 0 && _closedInfo is not null)
                _handOffsEnded.TrySetResult();
        }
    }

    private static async Task<T> InvokeAsync<T>(Func<GattRequest, Task<T>> send, GattRequest request) =>
        await send(request).ConfigureAwait(false);

    private void Complete(GattRequest request, Turn turn)
    {
        request.Complete();
        OnTransactionAnswered(request);
        if (request.IsAbandoned && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogAbandonedRequestCompleted(_address, _timeProvider.GetElapsedTime(request.StartedAt));
        Finish(turn);
    }

    private void OnTimer()
    {
        lock (_lock)
        {
            if (_closedInfo is not null || _transaction is null)
                return;
            // A timer armed for an earlier transaction may fire before the one waiting now is due
            TimeSpan remaining = AttTimeout - _timeProvider.GetElapsedTime(_sentAt);
            if (remaining > TimeSpan.Zero)
            {
                _timer.Change(remaining, Timeout.InfiniteTimeSpan);
                return;
            }
            _transaction = null;
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

    private enum TurnState
    {
        Waiting,
        Granted,
        Dropped,
    }

    /// <summary> An operation waiting for its turn in the send order. Its state changes under the queue's lock </summary>
    private sealed class Turn(bool isRequest)
    {
        public bool IsRequest { get; } = isRequest;

        public TurnState State { get; set; } = TurnState.Waiting;

        public TaskCompletionSource Granted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
