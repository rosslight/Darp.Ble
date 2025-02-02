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
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The newly added service </returns>
    public static async Task<IGattClientService> AddServiceAsync(
        this IBlePeripheral peripheral,
        ServiceDeclaration serviceDeclaration,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        ArgumentNullException.ThrowIfNull(serviceDeclaration);
        return await peripheral
            .AddServiceAsync(
                serviceDeclaration.Uuid,
                serviceDeclaration.Type is not GattServiceType.Secondary,
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
