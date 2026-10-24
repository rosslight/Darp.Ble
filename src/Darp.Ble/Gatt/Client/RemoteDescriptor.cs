using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A descriptor of a discovered characteristic. Its requests run with the connection's, in call order </summary>
public sealed class RemoteDescriptor
{
    private readonly ConnectedPeripheral _connection;
    private readonly IRemoteDescriptorDriver _driver;

    internal RemoteDescriptor(ConnectedPeripheral connection, IRemoteDescriptorDriver driver)
    {
        _connection = connection;
        _driver = driver;
        Uuid = driver.Uuid;
    }

    /// <summary> The UUID of the descriptor </summary>
    public BleUuid Uuid { get; }

    /// <summary> Reads the value </summary>
    /// <param name="cancellationToken"> Cancels the read; after it was sent, only the wait ends </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default) =>
        _connection.RunAsync(GattOperation.ReadDescriptor(Uuid), _driver.ReadAsync, cancellationToken);

    /// <summary> Writes the value </summary>
    /// <param name="value"> The value; copied, so the caller may reuse the buffer at once </param>
    /// <param name="cancellationToken"> Cancels the write; after it was sent, only the wait ends </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    /// <exception cref="BleDisconnectedException"> The connection ended </exception>
    public Task WriteAsync(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
    {
        byte[] copy = value.ToArray();
        return _connection.RunAsync(
            GattOperation.WriteDescriptor(Uuid),
            request => _driver.WriteAsync(copy, request),
            cancellationToken
        );
    }
}
