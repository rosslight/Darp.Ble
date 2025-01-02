using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.Ble.Data;
using NotSupportedException = System.NotSupportedException;

namespace Darp.Ble.Gatt.Client;

/// <summary> Class holding extensions for gatt client characteristics </summary>
public static class GattClientCharacteristicExtensions
{
    /// <summary> Add a characteristic with a Read property to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The characteristic to add </param>
    /// <param name="value"> The attribute value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<Properties.Read<T>>> AddCharacteristicAsync<T>(
        this IGattClientService service,
        Characteristic<Properties.Read<T>> characteristic,
        T value,
        CancellationToken cancellationToken = default)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        var buffer = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.TryWrite(buffer, value);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                new StaticAttributeValue(buffer),
                characteristic.Property,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<Properties.Read<T>>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a Read property to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The characteristic to add </param>
    /// <param name="value"> The attribute value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<Properties.Read>> AddCharacteristicAsync(
        this IGattClientService service,
        Characteristic<Properties.Read> characteristic,
        byte[] value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                new StaticAttributeValue(value),
                characteristic.Property,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<Properties.Read>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a Read property to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The characteristic to add </param>
    /// <param name="onWrite"> On write </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<Properties.Write>> AddCharacteristicAsync(
        this IGattClientService service,
        Characteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], GattProtocolStatus> onWrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                new FuncAttributeValue((_, _) => throw new NotSupportedException(), onWrite),
                characteristic.Property,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<Properties.Write>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a <see cref="Properties.Notify"/> property to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The characteristic to add </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<Properties.Notify>> AddCharacteristicAsync(
        this IGattClientService service,
        Characteristic<Properties.Notify> characteristic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                new FuncAttributeValue(
                    (_, _) => throw new NotSupportedException(),
                    (_,_,_) => throw new NotSupportedException()),
                characteristic.Property,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<Properties.Notify>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattClientCharacteristic<TProp1, TProp2>> AddCharacteristicAsync<TProp1, TProp2>(this IGattClientService service,
        BleUuid uuid,
        Func<IGattClientPeer, CancellationToken, ValueTask<byte[]>>? onRead = null,
        Func<IGattClientPeer, byte[], CancellationToken, ValueTask<GattProtocolStatus>>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                uuid,
                TProp1.GattProperty | TProp2.GattProperty,
                onRead,
                onWrite,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<TProp1, TProp2>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattTypedClientCharacteristic<T, TProp1, TProp2>> AddCharacteristicAsync<T, TProp1, TProp2>(this IGattClientService service,
        BleUuid uuid,
        Func<IGattClientPeer, CancellationToken, ValueTask<T>>? onRead = null,
        Func<IGattClientPeer, T, CancellationToken, ValueTask<GattProtocolStatus>>? onWrite = null,
        CancellationToken cancellationToken = default)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                uuid,
                TProp1.GattProperty,
                onRead.UsingBytes(),
                onWrite.UsingBytes(),
                cancellationToken)
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1, TProp2>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="value"> The attribute value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(this IGattClientService service,
        BleUuid uuid,
        IGattAttributeValue value,
        CancellationToken cancellationToken = default)
        where T : unmanaged
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                uuid,
                value,
                TProp1.GattProperty,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific ushort UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The ushort UUID of the characteristic to add </param>
    /// <param name="value"> The attribute value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        ushort uuid,
        IGattAttributeValue value,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return await service.AddCharacteristicAsync<TProp1>(new BleUuid(uuid), value, cancellationToken).ConfigureAwait(false);
    }
}