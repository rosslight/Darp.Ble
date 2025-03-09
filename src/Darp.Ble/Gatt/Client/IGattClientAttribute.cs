using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientAttribute123213
{
    /// <summary> Get the current value of the characteristic </summary>
    /// <param name="clientPeer"> The client peer to get the value for. If null, all subscribed clients will be taken into account </param>
    /// <returns> The current value </returns>
    ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer);

    /// <summary> Update the characteristic value </summary>
    /// <param name="clientPeer"> The client peer to update the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update with </param>
    /// <returns> The status of the update operation </returns>
    ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, ReadOnlyMemory<byte> value);
}
