using System.Reactive.Subjects;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <inheritdoc />
public abstract class GattServerCharacteristic(BleUuid uuid) : IGattServerCharacteristic
{
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken = default)
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
    public IConnectableObservable<byte[]> OnNotify()
    {
        return OnNotifyCore();
    }

    /// <summary>
    /// Core implementation to subscribe to notification events of the characteristic
    /// </summary>
    /// <returns> An observable which can be connected to, to start the subscription </returns>
    protected abstract IConnectableObservable<byte[]> OnNotifyCore();
}

/// <summary> The implementation of a strongly typed characteristic </summary>
/// <param name="serverCharacteristic"> The underlying characteristic </param>
/// <typeparam name="TProp1"> <inheritdoc cref="IGattServerCharacteristic{TProp1}"/> </typeparam>
public sealed class GattServerCharacteristic<TProp1>(IGattServerCharacteristic serverCharacteristic) : IGattServerCharacteristic<TProp1>
{
    /// <inheritdoc />
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}