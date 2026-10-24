using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> The services of a connected peripheral. Its requests run one at a time, in call order, with the connection's </summary>
public sealed class GattClient
{
    private readonly ConnectedPeripheral _connection;
    private readonly IConnectionDriver _driver;

    internal GattClient(ConnectedPeripheral connection, IConnectionDriver driver)
    {
        _connection = connection;
        _driver = driver;
    }

    /// <summary> Discovers the primary services of the peripheral </summary>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The services, in the peripheral's order </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public async Task<IReadOnlyList<RemoteGattService>> DiscoverServicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<IRemoteServiceDriver> services = await _connection
            .RunAsync(GattOperation.DiscoverServices, _driver.DiscoverServicesAsync, cancellationToken)
            .ConfigureAwait(false);
        return [.. services.Select(service => new RemoteGattService(_connection, service))];
    }

    /// <summary> Discovers a primary service of the peripheral </summary>
    /// <param name="uuid"> The UUID of the service </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The first service with the UUID </returns>
    /// <exception cref="GattException"> The peripheral has no such service (<see cref="GattErrorKind.NotFound"/>) or answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public async Task<RemoteGattService> DiscoverServiceAsync(
        BleUuid uuid,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(uuid);
        IReadOnlyList<RemoteGattService> services = await DiscoverServicesAsync(cancellationToken)
            .ConfigureAwait(false);
        return services.FirstOrDefault(service => service.Uuid == uuid)
            ?? throw new GattException(GattErrorKind.NotFound, $"The peripheral has no service {uuid}");
    }
}
