using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientCharacteristic(BleUuid uuid,
    GattProperty property)
    : GattClientCharacteristic(uuid, property)
{
    private readonly List<Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>>> _onWriteCallbacks = [];
    private readonly ConcurrentDictionary<IGattClientPeer, Action<byte[]>> _notifyActions = [];

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

    public async Task DisableNotificationsAsync(IGattClientPeer clientPeer)
    {
        await Task.Delay(200).ConfigureAwait(false);
        bool removedSuccessfully = _notifyActions.TryRemove(clientPeer, out _);
        Debug.Assert(removedSuccessfully, "This method should not be called if there is no callback to remove for this peer");
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