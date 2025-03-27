using System.Runtime.CompilerServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
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
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static GattTypedClientCharacteristic<T, TProp1> AddCharacteristic<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        OnReadAsyncCallback<T>? onRead = null,
        OnWriteAsyncCallback<T>? onWrite = null
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        OnReadAsyncCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, provider) =>
            {
                T value = await onRead(peer, provider).ConfigureAwait(false);
                return characteristicDeclaration.WriteValue(value);
            };
        OnWriteAsyncCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, provider) => onWrite(peer, characteristicDeclaration.ReadValue(bytes), provider);
        IGattClientCharacteristic characteristic = service.AddCharacteristic(
            TProp1.GattProperty,
            new FuncCharacteristicValue(
                characteristicDeclaration.Uuid,
                service.Peripheral.GattDatabase,
                onAsyncRead.CreateReadAccessPermissionFunc(),
                onAsyncRead,
                onAsyncWrite.CreateWriteAccessPermissionFunc(),
                onAsyncWrite
            ),
            []
        );
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
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattTypedClientCharacteristic<T, TProp1> AddCharacteristic<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        OnReadCallback<T>? onRead = null,
        OnWriteCallback<T>? onWrite = null
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        OnReadAsyncCallback<T>? onAsyncRead = onRead is null
            ? null
            : (peer, provider) => ValueTask.FromResult(onRead(peer, provider));
        OnWriteAsyncCallback<T>? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, provider) =>
            {
                if (bytes == null)
                    throw new ArgumentNullException(nameof(bytes));
                return ValueTask.FromResult(onWrite(peer, bytes, provider));
            };
        return service.AddCharacteristic(characteristicDeclaration, onAsyncRead, onAsyncWrite);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static GattTypedClientCharacteristic<T, TProp1, TProp2> AddCharacteristic<T, TProp1, TProp2>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        OnReadAsyncCallback<T>? onRead = null,
        OnWriteAsyncCallback<T>? onWrite = null
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        OnReadAsyncCallback? onAsyncRead = onRead is null
            ? null
            : async (peer, provider) =>
            {
                T value = await onRead(peer, provider).ConfigureAwait(false);
                return characteristicDeclaration.WriteValue(value);
            };
        OnWriteAsyncCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, provider) => onWrite(peer, characteristicDeclaration.ReadValue(bytes), provider);
        IGattClientCharacteristic characteristic = service.AddCharacteristic(
            TProp1.GattProperty | TProp2.GattProperty,
            new FuncCharacteristicValue(
                characteristicDeclaration.Uuid,
                service.Peripheral.GattDatabase,
                onAsyncRead.CreateReadAccessPermissionFunc(),
                onAsyncRead,
                onAsyncWrite.CreateWriteAccessPermissionFunc(),
                onAsyncWrite
            ),
            []
        );
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
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattTypedClientCharacteristic<T, TProp1> AddCharacteristic<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        T staticValue
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristic(
            characteristicDeclaration,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristicDeclaration"> The description of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattTypedClientCharacteristic<T, TProp1, TProp2> AddCharacteristic<T, TProp1, TProp2>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        T staticValue
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristic(
            characteristicDeclaration,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite;
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
    }
}
