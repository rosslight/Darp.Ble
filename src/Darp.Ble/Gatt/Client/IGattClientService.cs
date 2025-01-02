using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> The client service </summary>
public interface IGattClientService
{
    /// <summary> Defines the callback when the value should be read from the characteristic </summary>
    /// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A valueTask which holds the bytes of the characteristic value when completed </returns>
    public delegate ValueTask<byte[]> OnReadCallback(IGattClientPeer? clientPeer, CancellationToken cancellationToken);
    /// <summary> Defines the callback when the value should be read from the characteristic </summary>
    /// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A valueTask which holds the characteristic value when completed </returns>
    public delegate ValueTask<T> OnReadCallback<T>(IGattClientPeer? clientPeer, CancellationToken cancellationToken);

    /// <summary> Defines the callback when the value should be read from the characteristic </summary>
    /// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
    /// <param name="value"> The value to be written to the characteristic </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A valueTask which holds the status of the write operation when completed </returns>
    public delegate ValueTask<GattProtocolStatus> OnWriteCallback(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken);

    /// <summary> Defines the callback when the value should be read from the characteristic </summary>
    /// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
    /// <param name="value"> The value to be written to the characteristic </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A valueTask which holds the status of the write operation when completed </returns>
    public delegate ValueTask<GattProtocolStatus> OnWriteCallback<in T>(IGattClientPeer? clientPeer, T value, CancellationToken cancellationToken);

    /// <summary> The UUID of the client service </summary>
    BleUuid Uuid { get; }
    /// <summary> All characteristics of the client service </summary>
    IReadOnlyCollection<IGattClientCharacteristic> Characteristics { get; }

    /// <summary> Add a characteristic to the service </summary>
    /// <param name="uuid"> The UUID of the service to add </param>
    /// <param name="gattProperty"> The property of the service to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> An <see cref="IGattClientCharacteristic"/> </returns>
    Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        GattProperty gattProperty,
        OnReadCallback? onRead,
        IGattClientService.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    );
}