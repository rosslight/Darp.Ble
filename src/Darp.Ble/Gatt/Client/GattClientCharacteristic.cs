using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Client;

public abstract class GattClientCharacteristic(BleUuid uuid, GattProperty property) : IGattClientCharacteristic
{
    public BleUuid Uuid { get; } = uuid;
    public GattProperty Property { get; } = property;

    public IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return OnWriteCore(callback);
    }

    protected abstract IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback);

    public async Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        return await NotifyAsyncCore(clientPeer, source, cancellationToken);
    }

    protected abstract Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken);
}

public sealed class GattClientCharacteristic<TProp1>(IGattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    public IGattClientCharacteristic Characteristic { get; } = characteristic;
}