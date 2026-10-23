using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> Discovery by declaration </summary>
public static class ConnectedPeripheralExtensions
{
    /// <summary> Discovers the primary service a declaration describes </summary>
    /// <param name="connection"> The connection to the peripheral </param>
    /// <param name="declaration"> The declaration of the service </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The first service with the declared UUID </returns>
    /// <exception cref="GattException"> The peripheral has no such service or answered with an ATT error </exception>
    public static Task<RemoteGattService> DiscoverServiceAsync(
        this ConnectedPeripheral connection,
        ServiceDeclaration declaration,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(declaration);
        return connection.DiscoverServiceAsync(declaration.Uuid, cancellationToken);
    }
}
