using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary>
/// Provides extensions for the <see cref="IGattServerService"/>
/// </summary>
public static class GattServerServiceExtensions
{
    /// <summary> Discover a characteristic with a given <see cref="uuid"/> as <see cref="BleUuid"/> </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(uuid, cancellationToken);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }

    /// <summary> Discover a characteristic with a given <see cref="uuid"/> as <see cref="ushort"/> </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        ushort uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return service.DiscoverCharacteristicAsync<TProp1>(new BleUuid(uuid), cancellationToken);
    }

    /// <summary>
    /// Discover a characteristic with a given <see cref="characteristic"/> as <see cref="Characteristic{TProp1}"/>
    /// </summary>
    /// <param name="service"> The service to discover the characteristics on </param>
    /// <param name="characteristic"> The characteristic to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TProp1"> The type of the characteristic </typeparam>
    /// <returns> The discovered gatt server characteristic </returns>
    public static async Task<IGattServerCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(this IGattServerService service,
        Characteristic<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(characteristic.Uuid, cancellationToken);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }
}