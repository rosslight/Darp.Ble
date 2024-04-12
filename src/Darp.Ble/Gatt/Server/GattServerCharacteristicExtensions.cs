namespace Darp.Ble.Gatt.Server;

public static class GattServerCharacteristicExtensions
{
    public static IObservable<byte[]> OnNotify(this IGattServerCharacteristic<GattProperty.Notify> characteristic)
    {
        throw new NotImplementedException();
    }
    public static Task WriteAsync(this IGattServerCharacteristic<GattProperty.Write> characteristic, byte[] bytes)
    {
        throw new NotImplementedException();
    }
}