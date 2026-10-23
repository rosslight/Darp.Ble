using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic discovered on a connected peripheral </summary>
public sealed class RemoteGattCharacteristic
{
    private readonly ConnectedPeripheral _connection;
    private readonly IRemoteCharacteristicDriver _driver;

    internal RemoteGattCharacteristic(ConnectedPeripheral connection, IRemoteCharacteristicDriver driver)
    {
        _connection = connection;
        _driver = driver;
        Uuid = driver.Uuid;
        Properties = driver.Properties;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties { get; }

    /// <summary> Reads the value </summary>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The characteristic does not support the operation, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Read);
        return _connection.RunAsync(_driver.ReadAsync, cancellationToken);
    }

    /// <summary> Writes the value with a write request, which the peripheral answers </summary>
    /// <param name="value"> The value; copied, so the caller may reuse the buffer at once </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The characteristic does not support the operation, or the peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task WriteAsync(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
    {
        EnsureSupports(GattProperty.Write);
        // An abandoned write keeps running; the copy makes it independent of the caller's buffer
        byte[] copy = value.ToArray();
        return _connection.RunAsync(request => _driver.WriteAsync(copy, request), cancellationToken);
    }

    private void EnsureSupports(GattProperty property)
    {
        if (!Properties.HasFlag(property))
        {
            throw new GattException(
                GattErrorKind.PropertyMismatch,
                $"The characteristic {Uuid} does not support {property}"
            );
        }
    }
}
