namespace Darp.Ble.Gatt.Client;

public static class GattClientCharacteristicExtensions
{
    public static void Notify(this IGattClientCharacteristic<GattProperty.Notify> characteristic,
        IGattClientPeer clientPeer, byte[] source)
    {
        characteristic.Characteristic.Notify(clientPeer, source);
    }

    public static void NotifyAll(this IGattClientCharacteristic<GattProperty.Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }

    public static IDisposable UpdateReadAll<T>(this IGattClientCharacteristic<GattProperty.Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }
    public static IDisposable OnWrite(this IGattClientCharacteristic<GattProperty.Write> characteristic,
        IGattClientPeer clientPeer, Action<byte[]> callback)
    {
        return characteristic.Characteristic.OnWrite((peer, bytes) =>
        {
            if (peer != clientPeer) return;
            callback(bytes);
        });
    }
    public static IDisposable OnWrite(this IGattClientCharacteristic<GattProperty.Write> characteristic,
        Action<IGattClientPeer, byte[]> callback)
    {
        return characteristic.Characteristic.OnWrite(callback);
    }
}