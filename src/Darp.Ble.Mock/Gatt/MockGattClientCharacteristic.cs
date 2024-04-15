using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattClientCharacteristic(BleUuid uuid, GattProperty property)
    : GattClientCharacteristic(uuid, property)
{
    private readonly List<Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>>> _onWriteCallbacks = [];

    protected override IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        _onWriteCallbacks.Add(callback);
        return Disposable.Create((List: _onWriteCallbacks, Callback: callback), x => x.List.Remove(x.Callback));
    }
    public async Task WriteAsync(IGattClientPeer clientPeer, byte[] bytes, CancellationToken cancellationToken)
    {
        foreach (Func<IGattClientPeer,byte[],CancellationToken,Task<GattProtocolStatus>> onWriteCallback in _onWriteCallbacks)
        {
            await onWriteCallback(clientPeer, bytes, cancellationToken);
        }
    }

    protected override async Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //return await _serverCharacteristic.NotifyAsync(clientPeer, source, cancellationToken);
    }
}