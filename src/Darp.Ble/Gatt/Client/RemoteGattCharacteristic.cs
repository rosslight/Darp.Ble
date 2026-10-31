using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic discovered on a connected peripheral </summary>
public sealed class RemoteGattCharacteristic
{
    private readonly ConnectedPeripheral _connection;
    private readonly IRemoteCharacteristicDriver _driver;

    /// <summary> The connection the characteristic belongs to </summary>
    internal ConnectedPeripheral Connection => _connection;

    /// <summary> The driver object; the same for the same characteristic on every discovery of the connection </summary>
    internal IRemoteCharacteristicDriver Driver => _driver;

    internal RemoteGattCharacteristic(ConnectedPeripheral connection, IRemoteCharacteristicDriver driver)
    {
        _connection = connection;
        _driver = driver;
        Uuid = driver.Uuid;
        Properties = driver.Properties;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties { get; }

    /// <summary> Reads the value </summary>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The characteristic does not support the operation, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Read);
        return _connection.RunAsync(_driver.ReadAsync, cancellationToken);
    }

    /// <summary> Writes the value with a write request, which the peripheral answers </summary>
    /// <param name="value"> The value; copied, so the caller may reuse the buffer at once </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The characteristic does not support the operation, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task WriteAsync(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Write);
        // An abandoned write keeps running; the copy makes it independent of the caller's buffer
        byte[] copy = value.ToArray();
        return _connection.RunAsync(request => _driver.WriteAsync(copy, request), cancellationToken);
    }

    /// <summary> Writes the value with a write command, which the peripheral does not answer (D8) </summary>
    /// <remarks> Completes once the backend accepted the packet, so a loop of awaited writes is paced to what the radio can send.
    /// It keeps the call order of the connection's operations, but does not wait for the answer of a request sent before it </remarks>
    /// <param name="value"> The value; the caller may reuse the buffer once the task completed </param>
    /// <param name="cancellationToken"> Cancels the write until the backend accepted it </param>
    /// <returns> A task that completes once the backend accepted the write; not when the peripheral got it </returns>
    /// <exception cref="GattException"> The characteristic does not support writes without response </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public ValueTask WriteWithoutResponseAsync(
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default
    )
    {
        EnsureSupports(GattProperty.WriteWithoutResponse);
        return _connection.RunCommandAsync(_driver, value, cancellationToken);
    }

    /// <summary> Discovers the descriptors of the characteristic </summary>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The descriptors, in the peripheral's order </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public async Task<IReadOnlyList<RemoteDescriptor>> DiscoverDescriptorsAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<IRemoteDescriptorDriver> descriptors = await _connection
            .RunAsync(_driver.DiscoverDescriptorsAsync, cancellationToken)
            .ConfigureAwait(false);
        return [.. descriptors.Select(descriptor => new RemoteDescriptor(_connection, descriptor))];
    }

    /// <summary> Turns notifications on for this connection; does nothing when they are on, whoever enabled them </summary>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends and the device's answer still counts </param>
    /// <returns> A task that completes once notifications are on </returns>
    /// <exception cref="GattException"> The characteristic does not notify, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task EnableNotificationsAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Notify);
        return _connection.GetNotifications(_driver).EnableAsync(_connection, cancellationToken);
    }

    /// <summary> Turns notifications off; every open stream of the characteristic ends with an <see cref="InvalidOperationException"/> after its queued items </summary>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends and the device's answer still counts </param>
    /// <returns> A task that completes once notifications are off </returns>
    /// <exception cref="GattException"> The characteristic does not notify, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task DisableNotificationsAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Notify);
        return _connection.GetNotifications(_driver).DisableAsync(_connection, cancellationToken);
    }

    /// <summary> Starts a stream of the notifications from now on, with the default queue. Does no I/O </summary>
    /// <returns> The stream; enumerate it once notifications are enabled </returns>
    /// <exception cref="GattException"> The characteristic does not notify </exception>
    public NotificationStream<byte[]> Subscribe() => Subscribe(DefaultNotificationQueue);

    internal QueueOptions DefaultNotificationQueue => _connection.NotificationQueue;

    /// <summary> Starts a stream of the notifications from now on. Does no I/O </summary>
    /// <param name="queue"> The queue of this stream </param>
    /// <returns> The stream; enumerate it once notifications are enabled </returns>
    /// <exception cref="GattException"> The characteristic does not notify </exception>
    public NotificationStream<byte[]> Subscribe(QueueOptions queue) => Subscribe(queue, value => value);

    internal NotificationStream<T> Subscribe<T>(QueueOptions queue, Func<byte[], T> decode)
    {
        ArgumentNullException.ThrowIfNull(queue);
        EnsureSupports(GattProperty.Notify);
        return _connection.GetNotifications(_driver).Subscribe(queue, decode);
    }

    private void EnsureSupports(GattProperty property)
    {
        if (!Properties.HasFlag(property))
        {
            throw new GattException(
                GattErrorKind.PropertyMismatch,
                $"The characteristic {Uuid} does not support {property}"
            );
        }
    }
}
