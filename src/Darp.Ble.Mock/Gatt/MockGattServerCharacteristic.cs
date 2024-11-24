using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerCharacteristic(BleUuid uuid,
    IMockGattClientCharacteristic characteristic,
    MockGattClientPeer gattClient)
    : GattServerCharacteristic(uuid, logger: null)
{
    private readonly IMockGattClientCharacteristic _characteristic = characteristic;
    private readonly MockGattClientPeer _gattClient = gattClient;

    /// <inheritdoc />
    protected override Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        return _characteristic.WriteAsync(_gattClient, bytes, cancellationToken);
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state, Action<TState, byte[]> onNotify, CancellationToken cancellationToken)
    {
        await _characteristic.EnableNotificationsAsync(_gattClient, bytes => onNotify(state, bytes), cancellationToken)
            .ConfigureAwait(false);
        return Disposable.Empty;
    }

    protected override Task DisableNotificationsAsync() => _characteristic.DisableNotificationsAsync(_gattClient);
}