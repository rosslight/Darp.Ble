using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Server;

/// <inheritdoc />
public abstract class GattServerCharacteristic(GattServerService service,
    ushort attributeHandle,
    BleUuid uuid,
    GattProperty property,
    ILogger<GattServerCharacteristic> logger) : IGattServerCharacteristic
{
    private readonly SemaphoreSlim _notifySemaphore = new(1, 1);
    private IDisposable? _notifyDisposable;
    private readonly List<Action<byte[]>> _actions = [];

    /// <summary> The optional logger </summary>
    protected ILogger<GattServerCharacteristic> Logger { get; } = logger;
    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Service.Peer.Central.Device.LoggerFactory;

    /// <inheritdoc />
    public IGattServerService Service { get; } = service;

    /// <inheritdoc />
    public ushort AttributeHandle { get; } = attributeHandle;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattProperty Property { get; } = property;

    /// <inheritdoc />
    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await WriteAsyncCore(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void WriteWithoutResponse(byte[] bytes)
    {
        WriteWithoutResponseCore(bytes);
    }

    /// <summary>
    /// Core implementation to write bytes to the characteristic
    /// </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A Task which represents the operation </returns>
    protected abstract Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken);

    /// <summary> Core implementation to write bytes to the characteristic without waiting on a response </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    protected abstract void WriteWithoutResponseCore(byte[] bytes);

    /// <inheritdoc />
    public async Task<IAsyncDisposable> OnNotifyAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
    {
        Action<byte[]> action = bytes => onNotify(state, bytes);
        await _notifySemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (_notifyDisposable is null)
            {
                _notifyDisposable = await EnableNotificationsAsync(this, static (characteristic, bytes) =>
                {
                    // Reversed for loop. Actions might be removed from list on involke
                    for (int index = characteristic._actions.Count - 1; index >= 0; index--)
                    {
                        if (characteristic._actions.Count is 0)
                            return;
                        Action<byte[]> item1Action = characteristic._actions[index];
                        item1Action(bytes);
                    }
                }, cancellationToken).ConfigureAwait(false);
                Logger.LogTrace("Enabled notifications on {@Characteristic}", this);
            }
            _actions.Add(action);
        }
        finally
        {
            _notifySemaphore.Release();
        }
        return AsyncDisposable.Create(async () =>
        {
            await _notifySemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _actions.Remove(action);
                if (_actions.Count != 0)
                {
                    return;
                }
                _notifyDisposable.Dispose();
                _notifyDisposable = null;
                Logger.LogTrace("Starting to disable notifications on {@Characteristic}", this);
                await DisableNotificationsAsync().ConfigureAwait(false);
                Logger.LogTrace("Disabled notifications on {@Characteristic}", this);
            }
            finally
            {
                _notifySemaphore.Release();
            }
        });
    }

    /// <inheritdoc />
    public Task<byte[]> ReadAsync(CancellationToken cancellationToken)
    {
        return ReadAsyncCore(cancellationToken);
    }

    /// <inheritdoc cref="ReadAsync" />
    protected abstract Task<byte[]> ReadAsyncCore(CancellationToken cancellationToken);

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
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}

public sealed class TypedGattServerCharacteristic<T, TProp1>(IGattServerCharacteristic serverCharacteristic,
    Func<byte[], T> onRead,
    Func<T, byte[]> onWrite)
    : IGattServerCharacteristic<T, TProp1>
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;

    public Func<byte[], T> OnRead { get; } = onRead;
    public Func<T, byte[]> OnWrite { get; } = onWrite;
}