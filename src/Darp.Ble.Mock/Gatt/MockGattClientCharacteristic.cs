using System.Collections.Concurrent;
using System.Diagnostics;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientCharacteristic(
    MockGattClientService service,
    ushort attributeHandle,
    BleUuid uuid,
    GattProperty gattProperty,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite
) : GattClientCharacteristic(service, attributeHandle, uuid, gattProperty, onRead, onWrite)
{
    private readonly ConcurrentDictionary<IGattClientPeer, Action<byte[]>> _notifyActions = [];

    public async Task WriteAsync(
        IGattClientPeer? clientPeer,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        await UpdateValueAsync(clientPeer, bytes, cancellationToken).ConfigureAwait(false);
    }

    public Task EnableNotificationsAsync(
        IGattClientPeer clientPeer,
        Action<byte[]> onNotify,
        CancellationToken _
    )
    {
        bool newlyAdded = _notifyActions.TryAdd(clientPeer, onNotify);
        Debug.Assert(
            newlyAdded,
            "This method should not be called if a callback was added for this peer already"
        );
        return Task.CompletedTask;
    }

    public async Task DisableNotificationsAsync(IGattClientPeer clientPeer)
    {
        await Task.Delay(200).ConfigureAwait(false);
        bool removedSuccessfully = _notifyActions.TryRemove(clientPeer, out _);
        Debug.Assert(
            removedSuccessfully,
            "This method should not be called if there is no callback to remove for this peer"
        );
    }

    protected override Task<IGattClientDescriptor> AddDescriptorAsyncCore(
        BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<IGattClientDescriptor>(
            new MockGattClientDescriptor(this, uuid, onRead, onWrite)
        );
    }

    protected override void NotifyCore(IGattClientPeer clientPeer, byte[] value)
    {
        if (!_notifyActions.TryGetValue(clientPeer, out Action<byte[]>? action))
            return;
        action(value);
    }

    protected override Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}
