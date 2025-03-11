using System.Collections.Concurrent;
using System.Diagnostics;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientCharacteristic(
    MockGattClientService service,
    GattProperty properties,
    IGattCharacteristicValue value,
    ILogger<MockGattClientCharacteristic> logger
) : GattClientCharacteristic(service, properties, value, logger)
{
    private readonly ConcurrentDictionary<IGattClientPeer, Action<byte[]>> _notifyActions = [];

    public Task EnableNotificationsAsync(IGattClientPeer clientPeer, Action<byte[]> onNotify, CancellationToken _)
    {
        bool newlyAdded = _notifyActions.TryAdd(clientPeer, onNotify);
        Debug.Assert(newlyAdded, "This method should not be called if a callback was added for this peer already");
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

    protected override ValueTask NotifyAsyncCore(IGattClientPeer clientPeer, byte[] value)
    {
        if (!_notifyActions.TryGetValue(clientPeer, out Action<byte[]>? action))
            return ValueTask.CompletedTask;
        action(value);
        return ValueTask.CompletedTask;
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
