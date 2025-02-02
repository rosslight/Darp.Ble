namespace Darp.Ble.Gatt.Server;

/// <summary> Provides extensions for the <see cref="IGattServerService"/> </summary>
public static partial class GattServerServiceExtensions
{
    /// <summary>
    /// Discover a characteristic with a given <paramref name="characteristicDeclaration"/> as <see cref="CharacteristicDeclaration{TProp1}"/>
    /// </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="characteristicDeclaration"> The characteristic to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<TypedGattServerCharacteristic<T, TProp1>> DiscoverCharacteristicAsync<
        T,
        TProp1
    >(
        this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        IGattServerCharacteristic serverCharacteristic = await service
            .DiscoverCharacteristicAsync(characteristicDeclaration.Uuid, cancellationToken)
            .ConfigureAwait(false);
        if (!serverCharacteristic.Properties.HasFlag(TProp1.GattProperty))
        {
            throw new Exception(
                $"Discovered characteristic does not support property {TProp1.GattProperty}"
            );
        }
        return new TypedGattServerCharacteristic<T, TProp1>(
            serverCharacteristic,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue
        );
    }

    /// <summary>
    /// Discover a characteristic with a given <paramref name="characteristicDeclaration"/> as <see cref="CharacteristicDeclaration{TProp1}"/>
    /// </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="characteristicDeclaration"> The characteristic to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The type of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The type of the second characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<
        TypedGattServerCharacteristic<T, TProp1, TProp2>
    > DiscoverCharacteristicAsync<T, TProp1, TProp2>(
        this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        IGattServerCharacteristic serverCharacteristic = await service
            .DiscoverCharacteristicAsync(characteristicDeclaration.Uuid, cancellationToken)
            .ConfigureAwait(false);
        if (!serverCharacteristic.Properties.HasFlag(TProp1.GattProperty | TProp2.GattProperty))
        {
            throw new Exception(
                $"Discovered characteristic does not support property {TProp1.GattProperty | TProp2.GattProperty}"
            );
        }
        return new TypedGattServerCharacteristic<T, TProp1, TProp2>(
            serverCharacteristic,
            characteristicDeclaration.ReadValue,
            characteristicDeclaration.WriteValue
        );
    }
}
