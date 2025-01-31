using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary>
/// Provides extensions for the <see cref="IGattServerService"/>
/// </summary>
public static class GattServerServiceExtensions
{
    /// <summary> Discover a characteristic with a given <paramref name="uuid"/> as <see cref="BleUuid"/> </summary>
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
        ArgumentNullException.ThrowIfNull(service);
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(uuid, cancellationToken).ConfigureAwait(false);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }

    /// <summary>
    /// Discover a characteristic with a given <paramref name="characteristic"/> as <see cref="Characteristic{TProp1}"/>
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
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristic);
        IGattServerCharacteristic serverCharacteristic = await service.DiscoverCharacteristicAsync(characteristic.Uuid, cancellationToken).ConfigureAwait(false);
        return new GattServerCharacteristic<TProp1>(serverCharacteristic);
    }

    public static bool TryGetCharacteristic<TProp1>(this IGattServerService service,
        Characteristic<TProp1> expectedCharacteristic,
        [NotNullWhen(true)] out IGattServerCharacteristic<TProp1>? characteristic)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(expectedCharacteristic);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == expectedCharacteristic.Uuid
                && serverCharacteristic.Property == expectedCharacteristic.Property)
            {
                characteristic = new GattServerCharacteristic<TProp1>(serverCharacteristic);
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Tries to get a characteristic of a given type that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="expectedCharacteristic"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<T, TProp1>(this IGattServerService service,
        TypedCharacteristic<T, TProp1> expectedCharacteristic,
        [NotNullWhen(true)] out IGattServerCharacteristic<T, TProp1>? characteristic)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(expectedCharacteristic);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == expectedCharacteristic.Uuid
                && serverCharacteristic.Property == expectedCharacteristic.Property)
            {
                characteristic = new TypedGattServerCharacteristic<T, TProp1>(serverCharacteristic,
                    expectedCharacteristic.OnRead,
                    expectedCharacteristic.OnWrite);
                return true;
            }
        }
        characteristic = null;
        return false;
    }
}