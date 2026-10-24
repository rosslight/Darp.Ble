namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// A connection's wrappers of one kind of native GATT object, one per attribute handle, so every discovery returns
/// the same ones. Once the connection ended it wraps nothing more: its end hands every wrapper to disposal, and a
/// discovery that completes later releases its native objects instead.
/// Generic over the native and wrapper types so the bookkeeping builds and is tested without Windows
/// </summary>
internal sealed class AttributeWrappers<TNative, TWrapper>
    where TWrapper : class
{
    private readonly Func<TNative, ushort> _handleOf;
    private readonly Func<TNative, TWrapper> _wrap;
    private readonly Action<TNative> _release;
    private readonly Lock _lock = new();
    private readonly Dictionary<ushort, TWrapper> _byHandle = [];
    private bool _ended;

    /// <summary> Starts without wrappers </summary>
    /// <param name="handleOf"> The attribute handle of a native object </param>
    /// <param name="wrap"> Wraps a native object the first time its handle shows up </param>
    /// <param name="release"> Releases a native object that is not wrapped: a duplicate, or one that came too late </param>
    public AttributeWrappers(Func<TNative, ushort> handleOf, Func<TNative, TWrapper> wrap, Action<TNative> release)
    {
        _handleOf = handleOf;
        _wrap = wrap;
        _release = release;
    }

    /// <summary> The wrappers of what a discovery returned; null once the connection ended </summary>
    public IReadOnlyList<TWrapper>? Wrap(IReadOnlyList<TNative> natives)
    {
        // Under the lock, so a wrapper is either part of the end or never created
        lock (_lock)
        {
            if (_ended)
            {
                foreach (TNative native in natives)
                    _release(native);
                return null;
            }
            var wrappers = new TWrapper[natives.Count];
            for (var i = 0; i < natives.Count; i++)
            {
                TNative native = natives[i];
                if (_byHandle.TryGetValue(_handleOf(native), out TWrapper? known))
                {
                    _release(native);
                    wrappers[i] = known;
                    continue;
                }
                wrappers[i] = _wrap(native);
                _byHandle.Add(_handleOf(native), wrappers[i]);
            }
            return wrappers;
        }
    }

    /// <summary> The connection ended; returns every wrapper handed out, for the caller to release </summary>
    public IReadOnlyList<TWrapper> End()
    {
        lock (_lock)
        {
            _ended = true;
            return [.. _byHandle.Values];
        }
    }
}
