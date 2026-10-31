using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> One request on a connection, as its driver sees it. The core hands a connection's operations to the driver in call order </summary>
/// <remarks>
/// A request takes one or more ATT transactions, e.g. discovery page by page, and reports each one, so the core can time out
/// a peer that stops answering. A write without response has no transaction and gets its tokens directly
/// </remarks>
public sealed class GattRequest
{
    private readonly RequestQueue _queue;

    // Linked to the connection's end
    private readonly CancellationTokenSource _handOff;

    // Guards the fields below: the caller cancels while the driver sends and completes
    private readonly Lock _lock = new();

    // A transaction went out; from then on a cancel abandons the request instead of cancelling the hand-off
    private bool _sent;
    private bool _abandoned;

    // The driver's task ended; a later cancel does nothing, as the hand-off token is disposed
    private bool _completed;

    private GattRequest(RequestQueue queue, object turn, long startedAt, CancellationTokenSource handOff)
    {
        _queue = queue;
        Turn = turn;
        StartedAt = startedAt;
        _handOff = handOff;
    }

    /// <summary> The caller's cancellation until the backend accepted the operation's first packet </summary>
    /// <remarks> Pass it to waits before sending, e.g. for buffer space. Never pass it to a transaction the backend accepted </remarks>
    /// <remarks> It includes the end of the connection </remarks>
    public CancellationToken HandOffToken => _handOff.Token;

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

    internal object Turn { get; }

    internal long StartedAt { get; }

    /// <summary> The backend accepted a transaction of the request; the ATT timeout runs until its answer </summary>
    public void OnTransactionSent()
    {
        lock (_lock)
            _sent = true;
        _queue.OnTransactionSent(this);
    }

    /// <summary> The answer of the transaction sent last arrived </summary>
    public void OnTransactionAnswered() => _queue.OnTransactionAnswered(this);

    internal static GattRequest ForRequest(
        RequestQueue queue,
        object turn,
        long startedAt,
        CancellationToken connectionClosed
    ) => new(queue, turn, startedAt, CancellationTokenSource.CreateLinkedTokenSource(connectionClosed));

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
            _handOff.Cancel();
            return false;
        }
    }

    /// <summary> The driver's task of a request ended </summary>
    internal void Complete()
    {
        lock (_lock)
        {
            _completed = true;
            _handOff.Dispose();
        }
    }
}
