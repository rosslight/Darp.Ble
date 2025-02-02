using System.Runtime.CompilerServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

/// <summary> Gatt extensions </summary>
public static partial class GattCharacteristicExtensions
{
    /// <summary> Add a characteristic with a specific UUID to a service using asynchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(
        this IGattClientService service,
        CharacteristicDeclaration<TProp1> characteristic,
        IGattClientAttribute.OnReadCallback? onRead = null,
        IGattClientAttribute.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.AddCharacteristicAsync<TProp1>(
            characteristic.Uuid,
            onRead,
            onWrite,
            cancellationToken
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using synchronous read/write callbacks </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(
        this IGattClientService service,
        CharacteristicDeclaration<TProp1> characteristic,
        Func<IGattClientPeer?, byte[]>? onRead = null,
        Func<IGattClientPeer?, byte[], GattProtocolStatus>? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.AddCharacteristicAsync<TProp1>(
            characteristic.Uuid,
            onRead,
            onWrite,
            cancellationToken
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The UUID of the characteristic to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first property of the characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    [OverloadResolutionPriority(1)]
    public static Task<GattClientCharacteristic<TProp1, TProp2>> AddCharacteristicAsync<
        TProp1,
        TProp2
    >(
        this IGattClientService service,
        CharacteristicDeclaration<TProp1, TProp2> characteristic,
        IGattClientAttribute.OnReadCallback? onRead = null,
        IGattClientAttribute.OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.AddCharacteristicAsync<TProp1, TProp2>(
            characteristic.Uuid,
            onRead,
            onWrite,
            cancellationToken
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the property of the characteristic </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(
        this IGattClientService service,
        CharacteristicDeclaration<TProp1> characteristic,
        byte[] staticValue,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.AddCharacteristicAsync<TProp1>(
            characteristic.Uuid,
            staticValue,
            cancellationToken
        );
    }

    /// <summary> Add a characteristic with a specific UUID to a service using a static value </summary>
    /// <param name="service"> The service to add the characteristic to </param>
    /// <param name="characteristic"> The UUID of the characteristic to add </param>
    /// <param name="staticValue"> The initial static value </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first property </typeparam>
    /// <typeparam name="TProp2"> The type of the second property </typeparam>
    /// <returns> A gatt client characteristic </returns>
    public static Task<GattClientCharacteristic<TProp1, TProp2>> AddCharacteristicAsync<
        TProp1,
        TProp2
    >(
        this IGattClientService service,
        CharacteristicDeclaration<TProp1, TProp2> characteristic,
        byte[] staticValue,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.AddCharacteristicAsync<TProp1, TProp2>(
            characteristic.Uuid,
            staticValue,
            cancellationToken
        );
    }
}
