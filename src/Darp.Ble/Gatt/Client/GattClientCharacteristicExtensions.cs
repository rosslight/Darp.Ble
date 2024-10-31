using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> Class holding extensions for gatt client characteristics </summary>
public static class GattClientCharacteristicExtensions
{
    /// <summary> Add a characteristic to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The characteristic to add </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        Characteristic<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(characteristic.Uuid, characteristic.Property, cancellationToken).ConfigureAwait(false);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(uuid, TProp1.GattProperty, cancellationToken).ConfigureAwait(false);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific ushort UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The ushort UUID of the characteristic to add </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        ushort uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return await service.AddCharacteristicAsync<TProp1>(new BleUuid(uuid), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IGattClientCharacteristic.NotifyAsync"/>
    public static async Task NotifyAsync(this IGattClientCharacteristic<Properties.Notify> characteristic,
        IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        await characteristic.Characteristic.NotifyAsync(clientPeer, source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Notify all peers </summary>
    /// <param name="characteristic"> The characteristic to notify about </param>
    /// <param name="source"> The source of values to notify </param>
    public static void NotifyAll(this IGattClientCharacteristic<Properties.Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }

    /// <summary> Update all values to read from </summary>
    /// <param name="characteristic"> The characteristic to notify about </param>
    /// <param name="value"> The source of values to notify </param>
    /// <typeparam name="T"> The type param of the value </typeparam>
    /// <returns> An <see cref="IDisposable"/> to unsubscribe </returns>
    public static IDisposable UpdateReadAll<T>(this IGattClientCharacteristic<Properties.Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IGattClientCharacteristic.OnWrite"/>
    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], Task<GattProtocolStatus>> callback)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.OnWrite((peer, bytes, _) => callback(peer, bytes));
    }

    /// <inheritdoc cref="IGattClientCharacteristic.OnWrite"/>
    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.OnWrite(callback);
    }

    /// <inheritdoc cref="IGattClientCharacteristic.OnWrite"/>
    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], GattProtocolStatus> callback)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.OnWrite((peer, bytes, _) => Task.FromResult(callback(peer, bytes)));
    }
}