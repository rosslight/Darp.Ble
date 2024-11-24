using System.Reactive.Disposables;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <inheritdoc />
public abstract class GattServerCharacteristic(BleUuid uuid, ILogger? logger) : IGattServerCharacteristic
{
    private readonly SemaphoreSlim _notifySemaphore = new(1, 1);

    /// <summary> The optional logger </summary>
    protected ILogger? Logger { get; } = logger;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await WriteAsyncCore(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Core implementation to write bytes to the characteristic
    /// </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A Task which represents the operation </returns>
    protected abstract Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task<IDisposable> OnNotifyAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
    {
        IDisposable disposable = await EnableNotificationsAsync(state, onNotify, cancellationToken);
        Logger?.LogTrace("Enabled notifications on {@Characteristic}", this);
        return Disposable.Create(() =>
        {
            disposable.Dispose();
            _ = Task.Run(async () =>
            {
                await _notifySemaphore.WaitAsync(default(CancellationToken)).ConfigureAwait(false);
                try
                {
                    Logger?.LogTrace("Starting to disable notifications on {@Characteristic}", this);
                    await DisableNotificationsAsync();
                    Logger?.LogTrace("Disabled notifications on {@Characteristic}", this);
                }
                finally
                {
                    _notifySemaphore.Release();
                }
            }, default);
        });
    }

    /// <summary> Core implementation to subscribe to notification events of the characteristic </summary>
    /// <param name="state"> The state to be accessible when <paramref name="onNotify"/> is called </param>
    /// <param name="onNotify"> The callback to be called when a notification event was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the initial subscription process </param>
    /// <typeparam name="TState"> The type of the <paramref name="state"/> </typeparam>
    /// <returns> A task which completes when notifications are enabled. </returns>
    protected abstract Task<IDisposable> EnableNotificationsAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken);

    /// <summary> Core implementation to unsubscribe from notification events of the characteristic </summary>
    /// <returns> A value task </returns>
    protected abstract Task DisableNotificationsAsync();
}

/// <summary> The implementation of a strongly typed characteristic </summary>
/// <param name="serverCharacteristic"> The underlying characteristic </param>
/// <typeparam name="TProp1"> <inheritdoc cref="IGattServerCharacteristic{TProp1}"/> </typeparam>
public sealed class GattServerCharacteristic<TProp1>(IGattServerCharacteristic serverCharacteristic) : IGattServerCharacteristic<TProp1>
{
    /// <inheritdoc />
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}