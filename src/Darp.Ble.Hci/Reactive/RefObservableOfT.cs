using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace Darp.Ble.Hci.Reactive;

file sealed class RefObservable<T>(Func<IRefObserver<T>, IDisposable> onSubscribe) : IRefObservable<T>
    where T : allows ref struct
{
    private readonly Func<IRefObserver<T>, IDisposable> _onSubscribe = onSubscribe;

    /// <inheritdoc />
    public IDisposable Subscribe(IRefObserver<T> observer)
    {
        return _onSubscribe(observer);
    }
}

/// <summary> The implementation of a ref observable </summary>
/// <param name="state"> The state to be passed to the subscription callback </param>
/// <param name="onSubscribe"> The callback to be called when a new observer subscribes </param>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
/// <typeparam name="TState"> The object that holds the state </typeparam>
file sealed class RefObservable<T, TState>(TState state, Func<TState, IRefObserver<T>, IDisposable> onSubscribe)
    : IRefObservable<T>
    where T : allows ref struct
{
    private readonly TState _state = state;
    private readonly Func<TState, IRefObserver<T>, IDisposable> _onSubscribe = onSubscribe;

    /// <inheritdoc />
    public IDisposable Subscribe(IRefObserver<T> observer) => _onSubscribe(_state, observer);
}

public static partial class RefObservable
{
    /// <summary> Create a new ref observable </summary>
    /// <param name="onSubscribe"> The callback to be called when a new observer subscribes </param>
    /// <typeparam name="T"> The object that provides notification information. </typeparam>
    /// <returns> The implementation of the ref observable </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IRefObservable<T> Create<T>(Func<IRefObserver<T>, IDisposable> onSubscribe)
        where T : allows ref struct
    {
        return new RefObservable<T>(onSubscribe);
    }

    /// <summary> Create a new ref observable </summary>
    /// <param name="state"> The state to be passed to the subscription callback </param>
    /// <param name="onSubscribe"> The callback to be called when a new observer subscribes </param>
    /// <typeparam name="T"> The object that provides notification information. </typeparam>
    /// <typeparam name="TState"> The object that holds the state </typeparam>
    /// <returns> The implementation of the ref observable </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IRefObservable<T> Create<T, TState>(
        TState state,
        Func<TState, IRefObserver<T>, IDisposable> onSubscribe
    )
        where T : allows ref struct
    {
        return new RefObservable<T, TState>(state, onSubscribe);
    }

    /// <summary> Create a new ref observable </summary>
    /// <param name="error"> The error to be thrown on subscription </param>
    /// <typeparam name="T"> The object that provides notification information. </typeparam>
    /// <returns> The implementation of the ref observable </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IRefObservable<T> Throw<T>(Exception error)
        where T : allows ref struct
    {
        return new RefObservable<T, Exception>(
            error,
            (state, observer) =>
            {
                observer.OnError(state);
                return Disposable.Empty;
            }
        );
    }
}
