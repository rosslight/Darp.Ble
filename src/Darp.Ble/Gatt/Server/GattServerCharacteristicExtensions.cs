using System.Reactive.Subjects;

namespace Darp.Ble.Gatt.Server;

/// <summary> Extensions for <see cref="IGattServerCharacteristic"/> </summary>
public static class GattServerCharacteristicExtensions
{
    /// <inheritdoc cref="IGattServerCharacteristic.OnNotify"/>
    /// <param name="characteristic">The characteristic with notify property</param>
    public static IConnectableObservable<byte[]> OnNotify(this IGattServerCharacteristic<Properties.Notify> characteristic)
    {
        return characteristic.Characteristic.OnNotify();
    }

    /// <inheritdoc cref="IGattServerCharacteristic.WriteAsync"/>
    /// <param name="characteristic">The characteristic with notify property</param>
    /// <param name="bytes"> The array of bytes to be written </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    public static async Task WriteAsync(this IGattServerCharacteristic<Properties.Write> characteristic,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        await characteristic.Characteristic.WriteAsync(bytes, cancellationToken);
    }
}