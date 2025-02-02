using System.Runtime.CompilerServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

/// <summary> Gatt extensions </summary>
public static partial class GattCharacteristicExtensions
{
    /// <summary> Add a characteristic with a specific UUID to a service using asynchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        IGattClientAttribute.OnReadCallback<T>? onRead = null,
        IGattClientAttribute.OnWriteCallback<T>? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        IGattClientAttribute.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, token) =>
            {
                T value = await onRead(peer, token).ConfigureAwait(false);
                return characteristicDeclaration.WriteValue(value);
            };
        IGattClientAttribute.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, token) => onWrite(peer, characteristicDeclaration.ReadValue(bytes), token);
        IGattClientCharacteristic characteristic = await service
            .AddCharacteristicAsync(
                characteristicDeclaration.Uuid,
                TProp1.GattProperty,
                onAsyncRead,
                onAsyncWrite,
                cancellationToken
            )
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1>(
            characteristic,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using synchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        Func<IGattClientPeer?, T>? onRead = null,
        Func<IGattClientPeer?, T, GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientAttribute.OnReadCallback<T>? onAsyncRead = onRead is null
            ? null
            : (peer, _) => ValueTask.FromResult(onRead(peer));
        IGattClientAttribute.OnWriteCallback<T>? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, _) => ValueTask.FromResult(onWrite(peer, bytes));
        return service.AddCharacteristicAsync(characteristicDeclaration, onAsyncRead, onAsyncWrite, cancellationToken);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<GattTypedClientCharacteristic<T, TProp1, TProp2>> AddCharacteristicAsync<
        T,
        TProp1,
        TProp2
    >(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        IGattClientAttribute.OnReadCallback<T>? onRead = null,
        IGattClientAttribute.OnWriteCallback<T>? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        IGattClientAttribute.OnReadCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, token) =>
            {
                T value = await onRead(peer, token).ConfigureAwait(false);
                return characteristicDeclaration.WriteValue(value);
            };
        IGattClientAttribute.OnWriteCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, token) => onWrite(peer, characteristicDeclaration.ReadValue(bytes), token);
        IGattClientCharacteristic characteristic = await service
            .AddCharacteristicAsync(
                characteristicDeclaration.Uuid,
                TProp1.GattProperty,
                onAsyncRead,
                onAsyncWrite,
                cancellationToken
            )
            .ConfigureAwait(false);
        return new GattTypedClientCharacteristic<T, TProp1, TProp2>(
            characteristic,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1>> AddCharacteristicAsync<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        T staticValue,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync(
            characteristicDeclaration,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattTypedClientCharacteristic<T, TProp1, TProp2>> AddCharacteristicAsync<T, TProp1, TProp2>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        T staticValue,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristicAsync(
            characteristicDeclaration,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            },
            cancellationToken
        );
    }
}
