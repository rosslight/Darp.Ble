namespace Darp.Ble.Hci.Reactive;

file sealed class DoRefObserver<T>(
    IRefObserver<T> outerObserver,
    Action<T>? onValue,
    Action<Exception>? onError,
    Action? onCompleted
) : IRefObserver<T>
    where T : allows ref struct
{
    private readonly IRefObserver<T> _outerObserver = outerObserver;
    private readonly Action<T>? _onValue = onValue;
    private readonly Action<Exception>? _onError = onError;
    private readonly Action? _onCompleted = onCompleted;

    public void OnNext(T value)
    {
        _onValue?.Invoke(value);
        _outerObserver.OnNext(value);
    }

    public void OnError(Exception error)
    {
        _onError?.Invoke(error);
        _outerObserver.OnError(error);
    }

    public void OnCompleted()
    {
        _onCompleted?.Invoke();
        _outerObserver.OnCompleted();
    }
}

public static partial class RefObservable
{
    /// <summary>
    /// Invokes an action for each element in the observable sequence, and propagates all observer messages through the result sequence.
    /// This method can be used for debugging, logging, etc. of query behavior by intercepting the message stream to run arbitrary actions for messages on the pipeline.
    /// </summary>
    /// <param name="source">Source sequence.</param>
    /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
    /// <param name="onError">Action to invoke upon exceptional termination of the observable sequence.</param>
    /// <param name="onCompleted">Action to invoke upon graceful termination of the observable sequence.</param>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <returns>The source sequence with the side-effecting behavior applied.</returns>
    public static IRefObservable<T> Do<T>(
        this IRefObservable<T> source,
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null
    )
        where T : allows ref struct
    {
        return Create<T, (IRefObservable<T> Source, Action<T> OnNext, Action<Exception>? OnError, Action? OnCompleted)>(
            (source, onNext, onError, onCompleted),
            (state, observer) =>
                state.Source.Subscribe(new DoRefObserver<T>(observer, state.OnNext, state.OnError, state.OnCompleted))
        );
    }
}
