using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Server;

/// <summary> Extensions for gatt client attributes </summary>
public static class GattClientExtensions
{
    /// <summary> Tries to get a characteristic that was already added </summary>
    /// <param name="peripheral"> The peripheral the service belongs to </param>
    /// <param name="serviceDeclaration"> The service definition </param>
    /// <param name="service"> The resulting service. Null if not present </param>
    /// <returns> True, when the service was found; False, otherwise </returns>
    public static bool TryGetService(
        this IBlePeripheral peripheral,
        ServiceDeclaration serviceDeclaration,
        [NotNullWhen(true)] out IGattClientService? service
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        ArgumentNullException.ThrowIfNull(serviceDeclaration);
        foreach (IGattClientService clientService in peripheral.Services)
        {
            if (clientService.Uuid == serviceDeclaration.Uuid && clientService.Type == serviceDeclaration.Type)
            {
                service = clientService;
                return true;
            }
        }
        service = null;
        return false;
    }

    /// <summary> Get a service that was already added </summary>
    /// <param name="peripheral"> The peripheral the service belongs to </param>
    /// <param name="serviceDeclaration"> The service definition </param>
    /// <returns> The gatt client service </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static IGattClientService GetService(this IBlePeripheral peripheral, ServiceDeclaration serviceDeclaration)
    {
        if (!peripheral.TryGetService(serviceDeclaration, out IGattClientService? clientService))
            throw new Exception($"Service {serviceDeclaration.Uuid} not found");
        return clientService;
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
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        return characteristic;
    }
}
