using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> One operation on a connection, as its driver sees it. The core hands a connection's operations to the driver in call order </summary>
/// <remarks>
/// A request takes one or more ATT transactions, e.g. discovery page by page, and reports each one, so the core can time out
/// a peer that stops answering. A write without response has no transaction; its task completes once the backend accepted it.
/// The core reuses the object of a write without response, so a driver must not keep it after the task completed
/// </remarks>
public sealed class GattRequest
{
    private readonly RequestQueue _queue;
    private readonly Lock _lock = new();

    // A request owns a token source linked to the connection's end; a write without response passes the caller's token on
    private readonly CancellationTokenSource? _handOff;
    private CancellationToken _callerToken;
    private bool _sent;
    private bool _abandoned;
    private bool _completed;

    private GattRequest(
        RequestQueue queue,
        object? turn,
        long startedAt,
        CancellationTokenSource? handOff,
        CancellationToken connectionClosed
    )
    {
        _queue = queue;
        ConnectionClosedToken = connectionClosed;
        Turn = turn;
        StartedAt = startedAt;
        _handOff = handOff;
    }

    /// <summary> The caller's cancellation until the backend accepted the operation's first packet </summary>
    /// <remarks> Pass it to waits before sending, e.g. for buffer space. Never pass it to a transaction the backend accepted </remarks>
    public CancellationToken HandOffToken => _handOff?.Token ?? _callerToken;

    /// <summary> Cancelled when the connection ends </summary>
    /// <remarks> A request's <see cref="HandOffToken"/> includes it. A write without response waits for buffer space on both tokens, so the end of its connection does not wait for buffers that never free up </remarks>
    public CancellationToken ConnectionClosedToken { get; }

    /// <summary> Whether the caller gave up after the request's first transaction was sent </summary>
    /// <remarks> The caller already saw the cancellation. A request with several transactions checks it before each further one </remarks>
    public bool IsAbandoned
    {
        get
        {
            lock (_lock)
                return _abandoned;
        }
    }

    internal object? Turn { get; }

    internal long StartedAt { get; }

    internal bool IsRequest => _handOff is not null;

    /// <summary> The backend accepted a transaction of a request; the ATT timeout runs until its answer. A write without response ignores it </summary>
    public void OnTransactionSent()
    {
        if (!IsRequest)
            return;
        lock (_lock)
            _sent = true;
        _queue.OnTransactionSent(this);
    }

    /// <summary> The answer of the transaction sent last arrived. A write without response ignores it </summary>
    public void OnTransactionAnswered()
    {
        if (IsRequest)
            _queue.OnTransactionAnswered(this);
    }

    internal static GattRequest ForRequest(
        RequestQueue queue,
        object turn,
        long startedAt,
        CancellationToken connectionClosed
    ) =>
        new(
            queue,
            turn,
            startedAt,
            CancellationTokenSource.CreateLinkedTokenSource(connectionClosed),
            connectionClosed
        );

    /// <summary> The one object a connection reuses for its writes without response, which reach the driver one at a time </summary>
    internal static GattRequest ForCommands(RequestQueue queue, CancellationToken connectionClosed) =>
        new(queue, turn: null, startedAt: 0, handOff: null, connectionClosed);

    /// <summary> A write without response with this caller's token reaches the driver </summary>
    internal void PrepareCommand(CancellationToken callerToken) => _callerToken = callerToken;

    /// <summary> The caller cancelled: before sending, the driver stops waiting; afterwards the request runs to its end abandoned </summary>
    /// <returns> Whether the request was abandoned after sending </returns>
    internal bool Cancel()
    {
        lock (_lock)
        {
            if (_completed)
                return false;
            if (_sent)
            {
                _abandoned = true;
                return true;
            }
            _handOff?.Cancel();
            return false;
        }
    }

    /// <summary> The driver's task of a request ended </summary>
    internal void Complete()
    {
        lock (_lock)
        {
            _completed = true;
            _handOff?.Dispose();
        }
    }
}
