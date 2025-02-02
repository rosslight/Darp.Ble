using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> Provides extensions for the <see cref="IGattServerService"/> </summary>
public static partial class GattServerServiceExtensions
{
    /// <summary> Discover a characteristic with a given <paramref name="uuid"/> as <see cref="BleUuid"/> </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<GattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(
        this IGattServerService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(uuid, cancellationToken).ConfigureAwait(false);
        if (!serverCharacteristic.Properties.HasFlag(TProp1.GattProperty))
        {
            throw new Exception($"Discovered characteristic does not support property {TProp1.GattProperty}");
        }
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }

    /// <summary> Discover a characteristic with a given <paramref name="uuid"/> as <see cref="BleUuid"/> </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<GattServerCharacteristic<TProp1, TProp2>> DiscoverCharacteristicAsync<TProp1, TProp2>(
        this IGattServerService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(uuid, cancellationToken).ConfigureAwait(false);
        if (!serverCharacteristic.Properties.HasFlag(TProp1.GattProperty | TProp2.GattProperty))
        {
            throw new Exception($"Discovered characteristic does not support property {TProp1.GattProperty | TProp2.GattProperty}");
        }
        return new GattServerCharacteristic<TProp1, TProp2>(serverCharacteristic);
    }

    /// <summary>
    /// Discover a characteristic with a given <paramref name="characteristic"/> as <see cref="CharacteristicDeclaration{TProp1}"/>
    /// </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="characteristic"> The characteristic to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static Task<GattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(
        this IGattServerService service,
        CharacteristicDeclaration<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.DiscoverCharacteristicAsync<TProp1>(characteristic.Uuid, cancellationToken);
    }

    /// <summary>
    /// Discover a characteristic with a given <paramref name="characteristic"/> as <see cref="CharacteristicDeclaration{TProp1}"/>
    /// </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="characteristic"> The characteristic to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static Task<GattServerCharacteristic<TProp1, TProp2>> DiscoverCharacteristicAsync<TProp1, TProp2>(
        this IGattServerService service,
        CharacteristicDeclaration<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        return service.DiscoverCharacteristicAsync<TProp1, TProp2>(characteristic.Uuid, cancellationToken);
    }
}