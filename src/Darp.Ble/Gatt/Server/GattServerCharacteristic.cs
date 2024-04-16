using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public abstract class GattServerCharacteristic(BleUuid uuid) : IGattServerCharacteristic
{
    public BleUuid Uuid { get; } = uuid;

    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        await WriteAsyncCore(bytes, cancellationToken);
    }

    protected abstract Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken);

    public IObservable<byte[]> OnNotify()
    {
        return OnNotifyCore();
    }

    protected abstract IObservable<byte[]> OnNotifyCore();
}

public sealed class GattServerCharacteristic<TProp1>(IGattServerCharacteristic serverCharacteristic) : IGattServerCharacteristic<TProp1>
{
    public BleUuid Uuid => Characteristic.Uuid;
    public IGattServerCharacteristic Characteristic { get; } = serverCharacteristic;
}