using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gatt;

/// <summary> Extensions that belong to the gatt service </summary>
public static class GattServiceExtensions
{
    /// <summary> Discover a service specific to the given <paramref name="serviceDeclaration"/> of the peer peripheral </summary>
    /// <param name="peer"> The server peer to discover the service from </param>
    /// <param name="serviceDeclaration"> The serviceDeclaration that describes the service to discover </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A connection to the remote service </returns>
    public static async Task<IGattServerService> DiscoverServiceAsync(
        this IGattServerPeer peer,
        ServiceDeclaration serviceDeclaration,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(serviceDeclaration);
        IGattServerService service = await peer.DiscoverServiceAsync(serviceDeclaration.Uuid, cancellationToken)
            .ConfigureAwait(false);
        if (serviceDeclaration.Type is not GattServiceType.Undefined && service.Type != serviceDeclaration.Type)
            throw new Exception(
                $"Discovered service is of type {service.Type} and does not match declaration {serviceDeclaration.Type}"
            );
        return service;
    }

    /// <summary> Add a new service to this peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="serviceDeclaration"> The service declaration that specifies the service to be added </param>
    /// <returns> The newly added service </returns>
    public static IGattClientService AddService(this IBlePeripheral peripheral, ServiceDeclaration serviceDeclaration)
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        ArgumentNullException.ThrowIfNull(serviceDeclaration);
        return peripheral.AddService(serviceDeclaration.Uuid, serviceDeclaration.Type is not GattServiceType.Secondary);
    }

    /// <summary> Tries to get a characteristic that was already added </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        [NotNullWhen(true)] out GattTypedClientCharacteristic<T, TProp1>? characteristic
    )
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        foreach (IGattClientCharacteristic clientCharacteristic in service.Characteristics)
        {
            if (
                clientCharacteristic.Uuid == characteristicDeclaration.Uuid
                && clientCharacteristic.Properties.HasFlag(characteristicDeclaration.Properties)
            )
            {
                characteristic = new GattTypedClientCharacteristic<T, TProp1>(
                    clientCharacteristic,
                    characteristicDeclaration.ReadValue,
                    characteristicDeclaration.WriteValue
                );
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Get a characteristic that was already added </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> The gatt client characteristic </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static GattTypedClientCharacteristic<T, TProp1> GetCharacteristic<T, TProp1>(
        this IGattClientService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration
    )
        where TProp1 : IBleProperty
    {
        if (
            !service.TryGetCharacteristic(
                characteristicDeclaration,
                out GattTypedClientCharacteristic<T, TProp1>? characteristic
            )
        )
        {
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        }
        return characteristic;
    }
}
