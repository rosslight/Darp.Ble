namespace Darp.Ble;

/// <summary> Admits operations until it is closed; disposal closes it and waits for the operations it admitted </summary>
internal sealed class OperationTracker
{
    private readonly Lock _lock = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _running;
    private bool _closed;

    public bool IsClosed
    {
        get
        {
            lock (_lock)
                return _closed;
        }
    }

    /// <summary> Admits an operation; every successful call needs exactly one <see cref="Exit"/> </summary>
    /// <param name="owner"> The type named in the exception when the tracker is closed </param>
    /// <exception cref="ObjectDisposedException"> The tracker is closed </exception>
    public void Enter(Type owner)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_closed, owner);
            _running++;
        }
    }

    public void Exit()
    {
        lock (_lock)
        {
            _running--;
            if (_closed && _running == 0)
                _drained.TrySetResult();
        }
    }

    /// <summary> Admits nothing new from now on </summary>
    public void Close()
    {
        lock (_lock)
        {
            _closed = true;
            if (_running == 0)
                _drained.TrySetResult();
        }
    }

    /// <summary> Closes and waits until every admitted operation exited </summary>
    public Task CloseAndWaitAsync()
    {
        Close();
        return _drained.Task;
    }
}
