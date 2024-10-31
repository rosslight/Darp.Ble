using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client characteristic </summary>
public interface IGattClientCharacteristic
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }
    /// <summary> The property of the characteristic </summary>
    GattProperty Property { get; }
    /// <summary> Register a callback on a write request started by a central </summary>
    /// <param name="callback"> The callback to be called on write </param>
    /// <returns> An <see cref="IDisposable"/> to unsubscribe </returns>
    IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback);
    /// <summary> Notify an subscribed central </summary>
    /// <param name="clientPeer"> The client peer </param>
    /// <param name="source"> The byte array to send </param>
    /// <param name="cancellationToken"> A cancellation token to cancel the operation </param>
    /// <returns> A task which holds true, if successful </returns>
    Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken);
}

/// <summary> A gatt client characteristic with a single property </summary>
/// <typeparam name="TProperty1"> The type of the property </typeparam>
public interface IGattClientCharacteristic<TProperty1>
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid => Characteristic.Uuid;
    /// <summary> The gatt client characteristic </summary>
    IGattClientCharacteristic Characteristic { get; }
}