namespace Darp.Ble.Gatt.Server;

public static class GattServerCharacteristicExtensions
{
    public static IObservable<byte[]> OnNotify(this IGattServerCharacteristic<GattProperty.Notify> characteristic)
    {
        return characteristic.Characteristic.OnNotify();
    }

    public static async Task WriteAsync(this IGattServerCharacteristic<GattProperty.Write> characteristic, byte[] bytes)
    {
        await characteristic.Characteristic.WriteAsync(bytes);
    }
}