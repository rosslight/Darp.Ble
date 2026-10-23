using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Gatt.Services;

/// <summary> The query state of one echo service on one connection, shared by every wrapper of it </summary>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "The semaphore has no wait handle; the session lives as long as its connection"
)]
internal sealed class EchoSession
{
    private readonly Lock _lock = new();
    private bool _isBroken;

    /// <summary> One query at a time: replies carry no correlation </summary>
    public SemaphoreSlim Queries { get; } = new(1, 1);

    /// <summary> A query sent its request but got no reply; a late reply would be taken for the next query's </summary>
    public bool IsBroken
    {
        get
        {
            lock (_lock)
                return _isBroken;
        }
    }

    public void Break()
    {
        lock (_lock)
            _isBroken = true;
    }
}
