namespace Darp.Ble.Gatt;

/// <summary> The operations of discovered typed characteristics, one overload per position of the property marker </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The overloads differ in the characteristic or declaration they take, never in their optional parameters"
)]
public static class TypedRemoteCharacteristicExtensions
{
    /// <summary> Reads and decodes the value </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The decoded value </returns>
    public static Task<T> ReadAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Read> characteristic,
        CancellationToken cancellationToken = default
    ) => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <inheritdoc cref="ReadAsync{T}(ITypedRemoteCharacteristic{T, Properties.Read}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task<T> ReadAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Read, TOther> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <inheritdoc cref="ReadAsync{T}(ITypedRemoteCharacteristic{T, Properties.Read}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task<T> ReadAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Read> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => ReadCoreAsync(characteristic, characteristic?.Declaration, cancellationToken);

    /// <summary> Encodes and writes the value with a write request </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    public static Task WriteAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Write> characteristic,
        T value,
        CancellationToken cancellationToken = default
    ) => WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <inheritdoc cref="WriteAsync{T}(ITypedRemoteCharacteristic{T, Properties.Write}, T, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task WriteAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Write, TOther> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <inheritdoc cref="WriteAsync{T}(ITypedRemoteCharacteristic{T, Properties.Write}, T, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    public static Task WriteAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Write> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <summary> Turns notifications on for this connection; does nothing when they are on </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once notifications are on </returns>
    public static Task EnableNotificationsAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Notify> characteristic,
        CancellationToken cancellationToken = default
    ) => Untyped(characteristic).EnableNotificationsAsync(cancellationToken);

    /// <summary> Turns notifications off; every open stream of the characteristic ends </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once notifications are off </returns>
    public static Task DisableNotificationsAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.Notify> characteristic,
        CancellationToken cancellationToken = default
    ) => Untyped(characteristic).DisableNotificationsAsync(cancellationToken);

    /// <summary> Starts a stream of the decoded notifications from now on, with the default queue. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T>(
        this ITypedRemoteCharacteristic<T, Properties.Notify> characteristic
    ) => SubscribeCore(characteristic, characteristic?.Declaration, queue: null);

    /// <summary> Starts a stream of the decoded notifications from now on. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="queue"> The queue of this stream </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T>(
        this ITypedRemoteCharacteristic<T, Properties.Notify> characteristic,
        QueueOptions queue
    ) =>
        SubscribeCore(
            characteristic,
            characteristic?.Declaration,
            queue ?? throw new ArgumentNullException(nameof(queue))
        );

    /// <summary> Turns notifications on for this connection; does nothing when they are on </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once notifications are on </returns>
    public static Task EnableNotificationsAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Notify, TOther> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => Untyped(characteristic).EnableNotificationsAsync(cancellationToken);

    /// <summary> Turns notifications off; every open stream of the characteristic ends </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once notifications are off </returns>
    public static Task DisableNotificationsAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Notify, TOther> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => Untyped(characteristic).DisableNotificationsAsync(cancellationToken);

    /// <summary> Starts a stream of the decoded notifications from now on, with the default queue. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Notify, TOther> characteristic
    )
        where TOther : IBleProperty => SubscribeCore(characteristic, characteristic?.Declaration, queue: null);

    /// <summary> Starts a stream of the decoded notifications from now on. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="queue"> The queue of this stream </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.Notify, TOther> characteristic,
        QueueOptions queue
    )
        where TOther : IBleProperty =>
        SubscribeCore(
            characteristic,
            characteristic?.Declaration,
            queue ?? throw new ArgumentNullException(nameof(queue))
        );

    /// <summary> Turns notifications on for this connection; does nothing when they are on </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once notifications are on </returns>
    public static Task EnableNotificationsAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Notify> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => Untyped(characteristic).EnableNotificationsAsync(cancellationToken);

    /// <summary> Turns notifications off; every open stream of the characteristic ends </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="cancellationToken"> Cancels the change; after it was sent, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once notifications are off </returns>
    public static Task DisableNotificationsAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Notify> characteristic,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty => Untyped(characteristic).DisableNotificationsAsync(cancellationToken);

    /// <summary> Starts a stream of the decoded notifications from now on, with the default queue. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Notify> characteristic
    )
        where TOther : IBleProperty => SubscribeCore(characteristic, characteristic?.Declaration, queue: null);

    /// <summary> Starts a stream of the decoded notifications from now on. Does no I/O </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="queue"> The queue of this stream </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> The stream; values are decoded while it is read </returns>
    public static NotificationStream<T> Subscribe<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.Notify> characteristic,
        QueueOptions queue
    )
        where TOther : IBleProperty =>
        SubscribeCore(
            characteristic,
            characteristic?.Declaration,
            queue ?? throw new ArgumentNullException(nameof(queue))
        );

    /// <summary> Encodes and writes the value with a write command, which the peripheral does not answer </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write until the backend accepted it </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once the backend accepted the write </returns>
    public static ValueTask WriteWithoutResponseAsync<T>(
        this ITypedRemoteCharacteristic<T, Properties.WriteWithoutResponse> characteristic,
        T value,
        CancellationToken cancellationToken = default
    ) => WriteWithoutResponseCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <summary> Encodes and writes the value with a write command, which the peripheral does not answer </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write until the backend accepted it </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once the backend accepted the write </returns>
    public static ValueTask WriteWithoutResponseAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, Properties.WriteWithoutResponse, TOther> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteWithoutResponseCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    /// <summary> Encodes and writes the value with a write command, which the peripheral does not answer </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels the write until the backend accepted it </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once the backend accepted the write </returns>
    public static ValueTask WriteWithoutResponseAsync<T, TOther>(
        this ITypedRemoteCharacteristic<T, TOther, Properties.WriteWithoutResponse> characteristic,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        WriteWithoutResponseCoreAsync(characteristic, characteristic?.Declaration, value, cancellationToken);

    private static ValueTask WriteWithoutResponseCoreAsync<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        T value,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        return characteristic.Characteristic.WriteWithoutResponseAsync(declaration.Encode(value), cancellationToken);
    }

    private static RemoteGattCharacteristic Untyped(IRemoteCharacteristic? characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic;
    }

    private static NotificationStream<T> SubscribeCore<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        QueueOptions? queue
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        RemoteGattCharacteristic untyped = characteristic.Characteristic;
        return untyped.Subscribe(queue ?? untyped.DefaultNotificationQueue, value => declaration.Decode(value));
    }

    private static async Task<T> ReadCoreAsync<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        byte[] value = await characteristic.Characteristic.ReadAsync(cancellationToken).ConfigureAwait(false);
        return declaration.Decode(value);
    }

    private static Task WriteCoreAsync<T>(
        IRemoteCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        T value,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        return characteristic.Characteristic.WriteAsync(declaration.Encode(value), cancellationToken);
    }
}
