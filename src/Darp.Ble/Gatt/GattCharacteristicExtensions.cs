using System.Runtime.CompilerServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

/// <summary> Gatt extensions </summary>
public static partial class GattCharacteristicExtensions
{
     /// <summary> Add a characteristic with a specific UUID to a service using asynchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        BleUuid uuid,
        IGattClientService.OnReadCallback? onRead = null,
        IGattClientService.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                uuid,
                TProp1.GattProperty,
                onRead,
                onWrite,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }
    /// <summary> Add a characteristic with a specific UUID to a service using synchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static async Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        BleUuid uuid,
        Func<IGattClientPeer?, byte[]>? onRead = null,
        Func<IGattClientPeer?, byte[], GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientService.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : (peer, _) => ValueTask.FromResult(onRead(peer));
        IGattClientService.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, _) => ValueTask.FromResult(onWrite(peer, bytes));
        return await service.AddCharacteristicAsync<TProp1>(uuid, onAsyncRead, onAsyncWrite, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using asynchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        IGattClientService.OnReadCallback<T>? onRead = null,
        IGattClientService.OnWriteCallback<T>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientService.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, token) =>
            {
                T value = await onRead(peer, token).ConfigureAwait(false);
                return characteristic.OnWrite(value);
            };
        IGattClientService.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, token) => onWrite(peer, characteristic.OnRead(bytes), token);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                TProp1.GattProperty,
                onAsyncRead,
                onAsyncWrite,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1>(clientCharacteristic, characteristic.OnRead, characteristic.OnWrite);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using synchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        Func<IGattClientPeer?, T>? onRead = null,
        Func<IGattClientPeer?, T, GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientService.OnReadCallback<T>? onAsyncRead = onRead is null
            ? null
            : (peer, _) => ValueTask.FromResult(onRead(peer));
        IGattClientService.OnWriteCallback<T>? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, _) => ValueTask.FromResult(onWrite(peer, bytes));
        return service.AddCharacteristicAsync(characteristic, onAsyncRead, onAsyncWrite, cancellationToken);
    }

    public static async Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        Func<IGattClientPeer?, byte[]>? onRead = null,
        Func<IGattClientPeer?, byte[], GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientService.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : (peer, _) => ValueTask.FromResult(onRead(peer));
        IGattClientService.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, _) => ValueTask.FromResult(onWrite(peer, bytes));
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                TProp1.GattProperty,
                onAsyncRead,
                onAsyncWrite,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1>(clientCharacteristic, characteristic.OnRead, characteristic.OnWrite);
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
        IGattClientService.OnReadCallback? onRead = null,
        IGattClientService.OnWriteCallback? onWrite = null,
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
    /// <param name="characteristic"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattTypedClientCharacteristic<T, TProp1, TProp2>> AddCharacteristicAsync<T, TProp1, TProp2>(this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        IGattClientService.OnReadCallback<T>? onRead = null,
        IGattClientService.OnWriteCallback<T>? onWrite = null,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattClientService.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, token) =>
            {
                T value = await onRead(peer, token).ConfigureAwait(false);
                return characteristic.OnWrite(value);
            };
        IGattClientService.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, token) => onWrite(peer, characteristic.OnRead(bytes), token);
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(
                characteristic.Uuid,
                TProp1.GattProperty,
                onAsyncRead ,
                onAsyncWrite,
                cancellationToken)
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1, TProp2>(clientCharacteristic, characteristic.OnRead, characteristic.OnWrite);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        BleUuid uuid,
        byte[] staticValue,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync<TProp1>(uuid,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The description of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        T staticValue,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync(characteristic,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattClientCharacteristic<TProp1, TProp2>> AddCharacteristicAsync<TProp1, TProp2>(this IGattClientService service,
        BleUuid uuid,
        byte[] staticValue,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync<TProp1, TProp2>(uuid,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken: cancellationToken);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The description of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1, TProp2>> AddCharacteristicAsync<T, TProp1, TProp2>(this IGattClientService service,
        TypedCharacteristic<T, TProp1> characteristic,
        T staticValue,
        CancellationToken cancellationToken = default)
        where T : unmanaged
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync<T, TProp1, TProp2>(characteristic,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken);
    }
}