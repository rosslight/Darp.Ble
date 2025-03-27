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
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static GattClientCharacteristic<TProp1> AddCharacteristic<TProp1>(
        this IGattClientService service,
        BleUuid uuid,
        OnReadAsyncCallback? onRead = null,
        OnWriteAsyncCallback? onWrite = null
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = service.AddCharacteristic(
            TProp1.GattProperty,
            new FuncCharacteristicValue(
                uuid,
                service.Peripheral.GattDatabase,
                onRead.CreateReadAccessPermissionFunc(),
                onRead,
                onWrite.CreateWriteAccessPermissionFunc(),
                onWrite
            ),
            []
        );
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using synchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattClientCharacteristic<TProp1> AddCharacteristic<TProp1>(
        this IGattClientService service,
        BleUuid uuid,
        OnReadCallback? onRead = null,
        OnWriteCallback? onWrite = null
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        OnReadAsyncCallback? onAsyncRead = onRead is null
            ? null
            : (peer, provider) => ValueTask.FromResult(onRead(peer, provider));
        OnWriteAsyncCallback? onAsyncWrite = onWrite is null
            ? null
            : (peer, bytes, provider) => ValueTask.FromResult(onWrite(peer, bytes, provider));
        return service.AddCharacteristic<TProp1>(uuid, onAsyncRead, onAsyncWrite);
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static GattClientCharacteristic<TProp1, TProp2> AddCharacteristic<TProp1, TProp2>(
        this IGattClientService service,
        BleUuid uuid,
        OnReadAsyncCallback? onRead = null,
        OnWriteAsyncCallback? onWrite = null
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattClientCharacteristic clientCharacteristic = service.AddCharacteristic(
            TProp1.GattProperty | TProp2.GattProperty,
            new FuncCharacteristicValue(
                uuid,
                service.Peripheral.GattDatabase,
                onRead.CreateReadAccessPermissionFunc(),
                onRead,
                onWrite.CreateWriteAccessPermissionFunc(),
                onWrite
            ),
            []
        );
        return new GattClientCharacteristic<TProp1, TProp2>(clientCharacteristic);
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattClientCharacteristic<TProp1> AddCharacteristic<TProp1>(
        this IGattClientService service,
        BleUuid uuid,
        byte[] staticValue
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristic<TProp1>(
            uuid,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite.ToArray();
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="uuid"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static GattClientCharacteristic<TProp1, TProp2> AddCharacteristic<TProp1, TProp2>(
        this IGattClientService service,
        BleUuid uuid,
        byte[] staticValue
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddCharacteristic<TProp1, TProp2>(
            uuid,
            onRead: (_, _) => ValueTask.FromResult(staticValue),
            onWrite: (_, bytesToWrite, _) =>
            {
                staticValue = bytesToWrite.ToArray();
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
    }
}
