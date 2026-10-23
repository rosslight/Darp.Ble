namespace Darp.Ble.Gatt;

/// <summary> Discovery by declaration </summary>
public static class GattClientExtensions
{
    /// <summary> Discovers the primary service a declaration describes </summary>
    /// <param name="gatt"> The peripheral's services </param>
    /// <param name="declaration"> The declaration of the service </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The first service with the declared UUID </returns>
    /// <exception cref="GattException"> The peripheral has no such service or answered with an ATT error </exception>
    public static Task<RemoteGattService> DiscoverServiceAsync(
        this GattClient gatt,
        ServiceDeclaration declaration,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gatt);
        ArgumentNullException.ThrowIfNull(declaration);
        return gatt.DiscoverServiceAsync(declaration.Uuid, cancellationToken);
    }
}
