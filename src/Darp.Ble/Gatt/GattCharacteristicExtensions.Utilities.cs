using System.Runtime.InteropServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    private static T ToStruct<T>(this byte[] bytes) where T : unmanaged => MemoryMarshal.Read<T>(bytes);

    private static byte[] ToByteArray<T>(this T value)
        where T : unmanaged
    {
        var buffer = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(buffer, value);
        return buffer;
    }

    private static Func<IGattClientPeer, CancellationToken, ValueTask<byte[]>>? UsingBytes<T>(
        this Func<IGattClientPeer, CancellationToken, ValueTask<T>>? onRead)
        where T : unmanaged
    {
        return onRead is null
            ? null
            : async (peer, token) =>
            {
                T value = await onRead(peer, token).ConfigureAwait(false);
                return value.ToByteArray();
            };
    }

    private static Func<IGattClientPeer, byte[], CancellationToken, ValueTask<GattProtocolStatus>>? UsingBytes<T>(
        this Func<IGattClientPeer, T, CancellationToken, ValueTask<GattProtocolStatus>>? onWrite)
        where T : unmanaged
    {
        return onWrite is null
            ? null
            : (peer, bytes, token) => onWrite(peer, bytes.ToStruct<T>(), token);
    }

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