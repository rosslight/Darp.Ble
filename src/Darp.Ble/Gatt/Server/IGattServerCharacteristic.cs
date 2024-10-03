using System.Reactive.Subjects;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The interface defining a gatt server characteristic </summary>
public interface IGattServerCharacteristic
{
    /// <summary> The <see cref="BleUuid"/> of the characteristic </summary>
    BleUuid Uuid { get; }
    /// <summary> Write <paramref name="bytes"/> to the characteristic </summary>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A Task which represents the operation </returns>
    Task WriteAsync(byte[] bytes, CancellationToken cancellationToken = default);
    /// <summary> Subscribe to notification events of the characteristic </summary>
    /// <returns> An observable which can be connected to, to start the subscription </returns>
    IConnectableObservable<byte[]> OnNotify();
}

/// <summary> The interface defining a strongly typed characteristic </summary>
/// <typeparam name="TProp"> The property definition </typeparam>
public interface IGattServerCharacteristic<TProp>
{
    /// <inheritdoc cref="IGattServerCharacteristic.Uuid"/>
    BleUuid Uuid => Characteristic.Uuid;
    /// <summary> The underlying characteristic </summary>
    IGattServerCharacteristic Characteristic { get; }
}