using Darp.Ble.Connections;

namespace Darp.Ble.Backends;

/// <summary> One request on a connection, as its driver sees it. The core creates one per request and hands a connection's requests to the driver one at a time </summary>
/// <remarks> A request can take several ATT transactions, e.g. discovery page by page. The driver reports each one, so the core can time out a peer that stops answering </remarks>
public sealed class GattRequest
{
    private readonly RequestQueue _queue;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _handOff;
    private readonly long _startedAt;
    private bool _sent;
    private bool _abandoned;
    private bool _completed;

    internal GattRequest(RequestQueue queue, long startedAt, CancellationToken connectionClosed)
    {
        _queue = queue;
        _handOff = CancellationTokenSource.CreateLinkedTokenSource(connectionClosed);
        _startedAt = startedAt;
    }

    /// <summary> The caller's cancellation until the controller accepted the first transaction </summary>
    /// <remarks> Pass it to waits before sending, e.g. for buffer space. Never pass it to a transaction the controller accepted </remarks>
    public CancellationToken HandOffToken => _handOff.Token;

    /// <summary> Whether the caller gave up after the first transaction was sent </summary>
    /// <remarks> The caller already saw the cancellation. A request with several transactions checks it before each further one </remarks>
    public bool IsAbandoned
    {
        get
        {
            lock (_lock)
                return _abandoned;
        }
    }

    internal long StartedAt => _startedAt;

    /// <summary> The controller accepted a transaction; the ATT timeout runs until its answer </summary>
    public void OnTransactionSent()
    {
        lock (_lock)
            _sent = true;
        _queue.OnTransactionSent();
    }

    /// <summary> The answer of the transaction sent last arrived </summary>
    public void OnTransactionAnswered() => _queue.OnTransactionAnswered();

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

    /// <summary> The driver's task ended </summary>
    internal void Complete()
    {
        lock (_lock)
        {
            _completed = true;
            _handOff.Dispose();
        }
    }
}
