namespace Darp.Ble.Utils;

/// <summary> Helper methods for creating async disposables </summary>
public static class AsyncDisposable
{
    /// <summary> An empty async disposable </summary>
    public static IAsyncDisposable Empty { get; } =
        new AnonymousAsyncDisposable(() => ValueTask.CompletedTask);

    /// <summary> Create an async disposable from a disposable </summary>
    /// <param name="disposable"> The disposable to wrap </param>
    /// <returns> The async disposable </returns>
    public static IAsyncDisposable Create(IDisposable disposable)
    {
        return Create(disposable, d => d.Dispose());
    }

    /// <summary> Create an async disposable </summary>
    /// <param name="onDispose"> The dispose action expecting a ValueTask </param>
    /// <returns> The async disposable </returns>
    public static IAsyncDisposable Create(Func<ValueTask> onDispose) =>
        new AnonymousAsyncDisposable(onDispose);

    /// <summary> Create an async disposable from a sync callback</summary>
    /// <param name="onDispose"> The dispose action </param>
    /// <returns> The async disposable </returns>
    public static IAsyncDisposable Create(Action onDispose) =>
        Create(
            onDispose,
            x =>
            {
                x();
                return ValueTask.CompletedTask;
            }
        );

    /// <summary> Create an async disposable with a state</summary>
    /// <param name="state"> The state to be available in the onDispose callback </param>
    /// <param name="onDispose"> The dispose action expecting a ValueTask </param>
    /// <returns> The async disposable </returns>
    public static IAsyncDisposable Create<T>(T state, Func<T, ValueTask> onDispose) =>
        new AnonymousAsyncDisposable<T>(state, onDispose);

    /// <summary> Create an async disposable with a state and a sync callback </summary>
    /// <param name="state"> The state to be available in the onDispose callback </param>
    /// <param name="onDispose"> The dispose action </param>
    /// <returns> The async disposable </returns>
    public static IAsyncDisposable Create<T>(T state, Action<T> onDispose) =>
        Create(
            (state, onDispose),
            x =>
            {
                x.onDispose(x.state);
                return ValueTask.CompletedTask;
            }
        );
}
