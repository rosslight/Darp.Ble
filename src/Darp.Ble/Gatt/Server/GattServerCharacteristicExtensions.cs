using System.Reactive.Subjects;

namespace Darp.Ble.Gatt.Server;

public static class GattServerCharacteristicExtensions
{
    public static IConnectableObservable<byte[]> OnNotify(this IGattServerCharacteristic<Properties.Notify> characteristic)
    {
        return characteristic.Characteristic.OnNotify();
    }

    public static async Task WriteAsync(this IGattServerCharacteristic<Properties.Write> characteristic,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        await characteristic.Characteristic.WriteAsync(bytes, cancellationToken);
    }
}