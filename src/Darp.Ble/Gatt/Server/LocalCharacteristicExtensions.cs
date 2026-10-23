using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> Notifications of hosted characteristics, one overload per position of the Notify marker </summary>
[SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The overloads differ in the characteristic they take, never in their optional parameters"
)]
public static class LocalCharacteristicExtensions
{
    /// <summary> Notifies one central </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value; the caller may reuse the buffer once the task completed </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <returns> A task that completes once the backend accepted the notification </returns>
    public static ValueTask NotifyAsync(
        this ILocalCharacteristic<Properties.Notify> characteristic,
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.NotifyAsync(central, value, cancellationToken);
    }

    /// <summary> Encodes the value and notifies one central </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> A task that completes once the backend accepted the notification </returns>
    public static ValueTask NotifyAsync<T>(
        this ITypedLocalCharacteristic<T, Properties.Notify> characteristic,
        ConnectedCentral central,
        T value,
        CancellationToken cancellationToken = default
    ) => NotifyCoreAsync(characteristic, characteristic?.Declaration, central, value, cancellationToken);

    /// <summary> Encodes the value and notifies one central </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once the backend accepted the notification </returns>
    public static ValueTask NotifyAsync<T, TOther>(
        this ITypedLocalCharacteristic<T, Properties.Notify, TOther> characteristic,
        ConnectedCentral central,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        NotifyCoreAsync(characteristic, characteristic?.Declaration, central, value, cancellationToken);

    /// <summary> Encodes the value and notifies one central </summary>
    /// <param name="characteristic"> The characteristic </param>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TOther"> The other property marker </typeparam>
    /// <returns> A task that completes once the backend accepted the notification </returns>
    public static ValueTask NotifyAsync<T, TOther>(
        this ITypedLocalCharacteristic<T, TOther, Properties.Notify> characteristic,
        ConnectedCentral central,
        T value,
        CancellationToken cancellationToken = default
    )
        where TOther : IBleProperty =>
        NotifyCoreAsync(characteristic, characteristic?.Declaration, central, value, cancellationToken);

    private static ValueTask NotifyCoreAsync<T>(
        ILocalCharacteristic? characteristic,
        IGattTypedCharacteristic<T>? declaration,
        ConnectedCentral central,
        T value,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(declaration);
        return characteristic.Characteristic.NotifyAsync(central, declaration.Encode(value), cancellationToken);
    }
}
