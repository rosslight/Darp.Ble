namespace Darp.Ble.Gatt.Server;

public static class GattServerCharacteristicExtensions
{
    public static IObservable<byte[]> OnNotify(this IGattServerCharacteristic<Property.Notify> characteristic)
    {
        return characteristic.Characteristic.OnNotify();
    }

    public static async Task WriteAsync(this IGattServerCharacteristic<Property.Write> characteristic,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        await characteristic.Characteristic.WriteAsync(bytes, cancellationToken);
    }
}