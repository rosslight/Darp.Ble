using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientAttribute
{
    /// <summary> Get the current value of the characteristic </summary>
    /// <param name="clientPeer"> The client peer to get the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The current value </returns>
    ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken);
    /// <summary> Update the characteristic value </summary>
    /// <param name="clientPeer"> The client peer to update the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update with </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The status of the update operation </returns>
    ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken);

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
}