using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    private static byte[] GetValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer)
    {
        ValueTask<byte[]> getTask = characteristic.GetValueAsync(clientPeer, CancellationToken.None);
        return getTask.IsCompleted ? getTask.Result : getTask.AsTask().GetAwaiter().GetResult();
    }

    private static GattProtocolStatus UpdateValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer, byte[] value)
    {
        ValueTask<GattProtocolStatus> updateTask = characteristic.UpdateValueAsync(clientPeer, value, CancellationToken.None);
        return updateTask.IsCompleted ? updateTask.Result : updateTask.AsTask().GetAwaiter().GetResult();
    }

    private static void StartUpdateValue(this IGattClientCharacteristic characteristic, IGattClientPeer? clientPeer, byte[] value)
    {
        ValueTask<GattProtocolStatus> updateTask = characteristic.UpdateValueAsync(clientPeer, value, CancellationToken.None);
        if (updateTask.IsCompleted)
            return;
        _ = updateTask.AsTask();
    }
}