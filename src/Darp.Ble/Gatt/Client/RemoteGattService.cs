using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A service discovered on a connected peripheral </summary>
public sealed class RemoteGattService
{
    private readonly ConnectedPeripheral _connection;
    private readonly IRemoteServiceDriver _driver;

    internal RemoteGattService(ConnectedPeripheral connection, IRemoteServiceDriver driver)
    {
        _connection = connection;
        _driver = driver;
        Uuid = driver.Uuid;
    }

    internal ConnectedPeripheral Connection => _connection;

    /// <summary> The UUID of the service </summary>
    public BleUuid Uuid { get; }

    /// <summary> Discovers the characteristics of the service </summary>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The characteristics, in the peripheral's order </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public async Task<IReadOnlyList<RemoteGattCharacteristic>> DiscoverCharacteristicsAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<IRemoteCharacteristicDriver> characteristics = await _connection
            .RunAsync(
                GattOperation.DiscoverCharacteristics(Uuid),
                _driver.DiscoverCharacteristicsAsync,
                cancellationToken
            )
            .ConfigureAwait(false);
        return [.. characteristics.Select(characteristic => new RemoteGattCharacteristic(_connection, characteristic))];
    }
}
