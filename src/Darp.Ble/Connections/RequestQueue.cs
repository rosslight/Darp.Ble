using System.Diagnostics;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Diagnostics;
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

    private readonly string _backend;
    private readonly object _peer;
    private readonly DiagnosticsSettings _diagnostics;
    private readonly TimeProvider _timeProvider;
    private readonly Action<DisconnectInfo> _end;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _closed = new();
    private readonly ITimer _timer;
    private readonly TaskCompletionSource _handOffsEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly GattRequest _commandRequest;

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
    /// <param name="backend"> The backend, for spans and metrics </param>
    /// <param name="diagnostics"> What spans may contain </param>
    /// <param name="timeProvider"> The clock of the ATT timeout </param>
    /// <param name="end"> Ends the connection, when the peer stops answering </param>
    /// <param name="logger"> The logger </param>
    public RequestQueue(
        BleAddress address,
        string backend,
        DiagnosticsSettings diagnostics,
        TimeProvider timeProvider,
        Action<DisconnectInfo> end,
        ILogger logger
    )
    {
        _backend = backend;
        _peer = diagnostics.Peer(address);
        _diagnostics = diagnostics;
        _timeProvider = timeProvider;
        _end = end;
        _logger = logger;
        _commandRequest = GattRequest.ForCommands(this);
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
    /// <param name="operation"> What the request is, for its span and metric </param>
    /// <param name="send"> Sends the request through the driver </param>
    /// <param name="cancellationToken"> Gives up waiting. A request still queued is dropped; a sent one ends abandoned </param>
    /// <exception cref="BleDisconnectedException"> The connection ended before or while the request ran </exception>
    public async Task<T> RunAsync<T>(
        GattOperation operation,
        Func<GattRequest, Task<T>> send,
        CancellationToken cancellationToken
    )
    {
        // From enqueue to the end of the operation (D14); the driver's own spans nest inside
        var span = new Span(operation, operation.Start(_backend, _peer), _timeProvider.GetTimestamp());
        var turn = new Turn(isRequest: true);
        try
        {
            await WaitForTurnAsync(turn, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            End(span, Telemetry.OutcomeOf(e), e);
            throw;
        }
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
        // Owns the end of the operation, also after the caller gave up: telemetry first, then the turn, then the caller
        Task<T> finished = FinishAsync(answer, span, request, turn, registration);
        T result;
        try
        {
            result = await finished.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (request.Cancel())
                _logger.LogRequestAbandoned(_peer);
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
    /// <param name="operation"> What the write is, for its span and metric </param>
    /// <param name="driver"> The characteristic to write </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write until the backend accepted it </param>
    /// <remarks> With nothing queued and a driver that accepts at once, this allocates nothing </remarks>
    /// <exception cref="BleDisconnectedException"> The connection ended before the backend accepted the write </exception>
    public ValueTask RunCommandAsync(
        GattOperation operation,
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        Activity? previous = Activity.Current;
        var span = new Span(operation, operation.Start(_backend, _peer), _timeProvider.GetTimestamp());
        try
        {
            return RunCommandCore(span, driver, value, cancellationToken);
        }
        finally
        {
            // This method is not async, so the span must not stay current for the caller
            if (span.Activity is not null)
                Activity.Current = previous;
        }
    }

    private ValueTask RunCommandCore(
        Span span,
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        bool fast;
        lock (_lock)
        {
            if (_closedInfo is not null)
            {
                var disconnected = new BleDisconnectedException(_closedInfo);
                End(span, Telemetry.Disconnected, disconnected);
                return ValueTask.FromException(disconnected);
            }
            fast = _waiting.Count == 0 && _sending is null && !cancellationToken.IsCancellationRequested;
            if (fast)
            {
                _sending = _commandRequest;
                _invoking++;
                _unfinished++;
            }
        }
        return fast
            ? InvokeCommand(span, driver, value, turn: null, cancellationToken)
            : QueueCommandAsync(span, driver, value, cancellationToken);
    }

    /// <summary> Fails queued and new operations with the end of the connection; running ones end with the driver </summary>
    public void Close(DisconnectInfo info)
    {
        Turn[] dropped;
        lock (_lock)
        {
            if (_closedInfo is not null)
                return;
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
        Span span,
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        var turn = new Turn(isRequest: false);
        try
        {
            await WaitForTurnAsync(turn, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            End(span, Telemetry.OutcomeOf(e), e);
            throw;
        }
        await InvokeCommand(span, driver, value, turn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Hands a write without response to the driver; the operation holds the send order until the backend accepted it </summary>
    private ValueTask InvokeCommand(
        Span span,
        IRemoteCharacteristicDriver driver,
        ReadOnlyMemory<byte> value,
        Turn? turn,
        CancellationToken cancellationToken
    )
    {
        // Only the operation that holds the send order uses the shared request
        _commandRequest.PrepareCommand(cancellationToken);
        ValueTask accepted;
        try
        {
            accepted = driver.WriteWithoutResponseAsync(value, _commandRequest);
        }
        catch (Exception e)
        {
            FinishCommand(turn);
            Exception error = Translate(e, cancellationToken);
            End(span, Telemetry.OutcomeOf(error), error);
            return ValueTask.FromException(error);
        }
        finally
        {
            EndInvocation();
        }
        if (!accepted.IsCompletedSuccessfully)
            return AwaitCommandAsync(span, accepted, turn, cancellationToken);
        // Consumes the result, so a pooled source behind the task can be reused; it completed, so this never blocks
#pragma warning disable CA1849
        accepted.GetAwaiter().GetResult();
#pragma warning restore CA1849
        FinishCommand(turn);
        if (IsClosed)
        {
            BleDisconnectedException disconnected = Disconnected(innerException: null);
            End(span, Telemetry.Disconnected, disconnected);
            return ValueTask.FromException(disconnected);
        }
        End(span, Telemetry.Success, exception: null);
        return ValueTask.CompletedTask;
    }

    private async ValueTask AwaitCommandAsync(
        Span span,
        ValueTask accepted,
        Turn? turn,
        CancellationToken cancellationToken
    )
    {
        Exception? error = null;
        try
        {
            await accepted.ConfigureAwait(false);
            if (IsClosed)
                error = Disconnected(innerException: null);
        }
        catch (Exception e)
        {
            error = Translate(e, cancellationToken);
        }
        finally
        {
            FinishCommand(turn);
        }
        End(span, error is null ? Telemetry.Success : Telemetry.OutcomeOf(error), error);
        if (error is not null)
            throw error;
    }

    /// <summary> Waits for the driver's task, then ends the request's span and gives up its turn, before the caller learns the result </summary>
    private async Task<T> FinishAsync<T>(
        Task<T> answer,
        Span span,
        GattRequest request,
        Turn turn,
        CancellationTokenRegistration registration
    )
    {
        Exception? error = null;
        try
        {
            return await answer.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            error = e;
            throw;
        }
        finally
        {
            string outcome =
                IsClosed ? Telemetry.Disconnected
                : request.IsAbandoned || error is OperationCanceledException ? Telemetry.Cancelled
                : error is null ? Telemetry.Success
                : Telemetry.OutcomeOf(error);
            End(span, outcome, error);
            await registration.DisposeAsync().ConfigureAwait(false);
            Complete(request, turn);
        }
    }

    private void End(Span span, string outcome, Exception? exception)
    {
        Telemetry.Finish(span.Activity, outcome, exception, _diagnostics);
        Telemetry.RequestDuration.Record(
            _timeProvider.GetElapsedTime(span.Started).TotalSeconds,
            new TagList
            {
                { Telemetry.BackendTag, _backend },
                { Telemetry.OperationTag, span.Operation.Name },
                { Telemetry.OutcomeTag, outcome },
            }
        );
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
            if (ReferenceEquals(_sending, _commandRequest))
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
            _logger.LogAbandonedRequestCompleted(_peer, _timeProvider.GetElapsedTime(request.StartedAt));
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
        _logger.LogAttTimeout(_peer);
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

    /// <summary> The telemetry of one operation: its span, nothing when nobody listens, and when it was queued </summary>
    private readonly record struct Span(GattOperation Operation, Activity? Activity, long Started);

    /// <summary> An operation waiting for its turn in the send order. Its state changes under the queue's lock </summary>
    private sealed class Turn(bool isRequest)
    {
        public bool IsRequest { get; } = isRequest;

        public TurnState State { get; set; } = TurnState.Waiting;

        public TaskCompletionSource Granted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
