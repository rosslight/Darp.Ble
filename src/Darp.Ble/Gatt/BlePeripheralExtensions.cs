using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

/// <summary> Extensions for peripheral </summary>
public static class BlePeripheralExtensions
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
}
