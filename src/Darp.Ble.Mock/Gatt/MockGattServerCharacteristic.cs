using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerCharacteristic(BleUuid uuid,
    MockGattClientCharacteristic characteristic,
    MockGattServerService serverService)
    : GattServerCharacteristic(uuid)
{
    private readonly MockGattClientCharacteristic _characteristic = characteristic;
    private readonly List<IObserver<byte[]>> _onNotifyObservers = [];
    private readonly MockGattServerService _serverService = serverService;

    /// <inheritdoc />
    protected override Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        return _characteristic.WriteAsync(_serverService.GattClient, bytes, cancellationToken);
    }

    /// <inheritdoc />
    protected override IConnectableObservable<byte[]> OnNotifyCore() => Observable.Create<byte[]>(observer =>
    {
        _onNotifyObservers.Add(observer);
        return Disposable.Create((Observers: _onNotifyObservers, Observer: observer), x => x.Observers.Remove(observer));
    }).Publish();

    public Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        // Use inverse for loop as observers might be removed from list
        for (int index = _onNotifyObservers.Count - 1; index >= 0; index--)
        {
            IObserver<byte[]> onNotifyObserver = _onNotifyObservers[index];
            onNotifyObserver.OnNext(source);
        }

        return Task.FromResult(true);
    }
}