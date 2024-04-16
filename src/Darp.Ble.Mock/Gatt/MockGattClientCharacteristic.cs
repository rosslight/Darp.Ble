using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientCharacteristic(
    BleUuid uuid,
    GattProperty property,
    MockGattClientService clientService)
    : GattClientCharacteristic(uuid, property)
{
    public MockGattClientService ClientService { get; } = clientService;
    private readonly List<Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>>> _onWriteCallbacks = [];

    protected override IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        _onWriteCallbacks.Add(callback);
        return Disposable.Create((List: _onWriteCallbacks, Callback: callback), x => x.List.Remove(x.Callback));
    }
    public async Task WriteAsync(IGattClientPeer clientPeer, byte[] bytes, CancellationToken cancellationToken)
    {
        // Use inverse for loop as observers might be removed from list
        for (int index = _onWriteCallbacks.Count - 1; index >= 0; index--)
        {
            Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> onWriteCallback =
                _onWriteCallbacks[index];
            await onWriteCallback(clientPeer, bytes, cancellationToken);
        }
    }

    protected override async Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        if (clientPeer is not MockGattClientPeer mockClientPeer) return false;
        IGattServerService serverService = await mockClientPeer.GetService(ClientService.Uuid).FirstAsync().ToTask(cancellationToken);
        if (!serverService.Characteristics.TryGetValue(Uuid, out IGattServerCharacteristic? characteristic)
            || characteristic is not MockGattServerCharacteristic serverCharacteristic)
        {
            return false;
        }
        return await serverCharacteristic.NotifyAsync(clientPeer, source, cancellationToken);
    }
}