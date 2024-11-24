using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

internal interface IMockGattClientCharacteristic
{
    Task WriteAsync(IGattClientPeer clientPeer, byte[] bytes, CancellationToken cancellationToken);
    Task EnableNotificationsAsync(IGattClientPeer clientPeer, Action<byte[]> onNotify, CancellationToken cancellationToken);
    Task DisableNotificationsAsync(IGattClientPeer clientPeer);
}

internal sealed class MockGattClientCharacteristic(BleUuid uuid,
    GattProperty property)
    : GattClientCharacteristic(uuid, property), IMockGattClientCharacteristic
{
    private readonly List<Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>>> _onWriteCallbacks = [];
    private ConcurrentDictionary<IGattClientPeer, Action<byte[]>> _notifyActions = [];

    public async Task WriteAsync(IGattClientPeer clientPeer, byte[] bytes, CancellationToken cancellationToken)
    {
        // Use inverse for loop as observers might be removed from list
        for (int index = _onWriteCallbacks.Count - 1; index >= 0; index--)
        {
            Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> onWriteCallback =
                _onWriteCallbacks[index];
            await onWriteCallback(clientPeer, bytes, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task EnableNotificationsAsync(IGattClientPeer clientPeer, Action<byte[]> onNotify, CancellationToken cancellationToken)
    {
        bool newlyAdded = _notifyActions.TryAdd(clientPeer, onNotify);
        Debug.Assert(newlyAdded, "This method should not be called if a callback was added for this peer already");
        return Task.CompletedTask;
    }

    public Task DisableNotificationsAsync(IGattClientPeer clientPeer)
    {
        bool removedSuccessfully = _notifyActions.TryRemove(clientPeer, out _);
        Debug.Assert(removedSuccessfully, "This method should not be called if there is no callback to remove for this peer");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        _onWriteCallbacks.Add(callback);
        return Disposable.Create((List: _onWriteCallbacks, Callback: callback), x => x.List.Remove(x.Callback));
    }

    /// <inheritdoc />
    protected override Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        if (!_notifyActions.TryGetValue(clientPeer, out Action<byte[]>? action))
            return Task.FromResult(false);
        action(source);
        return Task.FromResult(true);
    }
}