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
    Task EnableNotificationsAsync(Action<byte[]> onNotify, CancellationToken cancellationToken);
    Task DisableNotificationsAsync();
}

internal sealed class MockGattClientCharacteristic(BleUuid uuid,
    GattProperty property)
    : GattClientCharacteristic(uuid, property), IMockGattClientCharacteristic
{
    private readonly List<Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>>> _onWriteCallbacks = [];
    private Action<byte[]>? _notifyAction;

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

    public Task EnableNotificationsAsync(Action<byte[]> onNotify, CancellationToken cancellationToken)
    {
        _notifyAction = onNotify;
        return Task.CompletedTask;
    }

    public Task DisableNotificationsAsync()
    {
        _notifyAction = null;
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
        if (_notifyAction is null)
            return Task.FromResult(false);
        _notifyAction(source);
        return Task.FromResult(true);
    }
}