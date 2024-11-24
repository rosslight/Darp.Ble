using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The interface defining a gatt server characteristic </summary>
public interface IGattServerCharacteristic
{
    /// <summary> The <see cref="BleUuid"/> of the characteristic </summary>
    BleUuid Uuid { get; }
    /// <summary> Write <paramref name="bytes"/> to the characteristic </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A Task which represents the operation </returns>
    Task WriteAsync(byte[] bytes, CancellationToken cancellationToken);

    /// <summary> Subscribe to notification events </summary>
    /// <param name="state"> The state to be accessible when <paramref name="onNotify"/> is called </param>
    /// <param name="onNotify"> The callback to be called when a notification event was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial subscription process </param>
    /// <typeparam name="TState"> The type of the <paramref name="state"/> </typeparam>
    /// <returns>
    /// A task which completes when notifications are enabled.
    /// Contains an <see cref="IDisposable"/> which can be used to unsubscribe from notifications.
    /// </returns>
    Task<IAsyncDisposable> OnNotifyAsync<TState>(TState state, Action<TState, byte[]> onNotify, CancellationToken cancellationToken);

/*
    /// <summary>
    /// Read a specific value from the
    /// </summary>
    /// <param name="state"> The state to be accessible when <paramref name="onRead"/> is called </param>
    /// <param name="onRead"> The callback to be called when the response was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <typeparam name="TState"> The type of the <paramref name="state"/> </typeparam>
    /// <returns> A task which completes when the value was read </returns>
    Task ReadAsync<TState>(TState state, Action<TState, ReadOnlyMemory<byte>> onRead, CancellationToken cancellationToken);

    /// <summary> Subscribe to indication events </summary>
    /// <param name="state"> The state to be accessible when <paramref name="onIndicate"/> is called </param>
    /// <param name="onIndicate"> The callback to be called when an indication event was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial subscription process </param>
    /// <typeparam name="TState"> The type of the <paramref name="state"/> </typeparam>
    /// <returns>
    /// A task which completes when indications are enabled.
    /// Contains an <see cref="IDisposable"/> which can be used to unsubscribe from indications.
    /// </returns>
    Task<IDisposable> OnIndicateAsync<TState>(TState state, Func<TState, ReadOnlyMemory<byte>, bool> onIndicate, CancellationToken cancellationToken);
*/
}

/// <summary> The interface defining a strongly typed characteristic </summary>
/// <typeparam name="TProp"> The property definition </typeparam>
public interface IGattServerCharacteristic<TProp>
{
    /// <inheritdoc cref="IGattServerCharacteristic.Uuid"/>
    BleUuid Uuid => Characteristic.Uuid;
    /// <summary> The underlying characteristic </summary>
    IGattServerCharacteristic Characteristic { get; }
}