using System.Reactive.Linq;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public abstract class GattServerCharacteristic(BleUuid uuid) : IGattServerCharacteristic
{
    public BleUuid Uuid { get; } = uuid;

    public async Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await WriteInternalAsync(bytes, cancellationToken);
    }

    protected abstract Task WriteInternalAsync(byte[] bytes, CancellationToken cancellationToken);

    public IObservable<byte[]> OnNotify()
    {
        return Observable.Empty<byte[]>();
    }
}