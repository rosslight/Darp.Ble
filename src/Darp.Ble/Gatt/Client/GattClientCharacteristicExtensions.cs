namespace Darp.Ble.Gatt.Client;

public static class GattClientCharacteristicExtensions
{
    public static void Notify(this IGattClientCharacteristic<GattProperty.Notify> characteristic,
        IGattClientDevice clientDevice, byte[] source)
    {
        throw new NotImplementedException();
    }

    public static IDisposable NotifyAll(this IGattClientCharacteristic<GattProperty.Notify> characteristic,
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
        Action<IGattClientDevice, byte[]> callback)
    {
        throw new NotImplementedException();
    }
}