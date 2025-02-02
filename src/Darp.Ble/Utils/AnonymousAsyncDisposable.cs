namespace Darp.Ble.Utils;

/// <summary> An async disposable </summary>
internal sealed class AnonymousAsyncDisposable<T>(T state, Func<T, ValueTask> onDispose)
    : IAsyncDisposable
{
    private readonly T _state = state;
    private Func<T, ValueTask>? _onDispose = onDispose;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<T, ValueTask>? dispose = Interlocked.Exchange(ref _onDispose, value: null);
        return dispose?.Invoke(_state) ?? ValueTask.CompletedTask;
    }
}

/// <summary> An async disposable </summary>
internal sealed class AnonymousAsyncDisposable(Func<ValueTask> onDispose) : IAsyncDisposable
{
    private Func<ValueTask>? _onDispose = onDispose;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<ValueTask>? dispose = Interlocked.Exchange(ref _onDispose, value: null);
        return dispose?.Invoke() ?? ValueTask.CompletedTask;
    }
}
