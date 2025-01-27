using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerCharacteristic(MockGattServerService service,
    BleUuid uuid,
    MockGattClientCharacteristic characteristic,
    MockGattClientPeer gattClient,
    ILogger<MockGattServerCharacteristic> logger)
    : GattServerCharacteristic(service, uuid, logger)
{
    private readonly MockGattClientCharacteristic _characteristic = characteristic;
    private readonly MockGattClientPeer _gattClient = gattClient;

    /// <inheritdoc />
    protected override Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        return _characteristic.WriteAsync(_gattClient, bytes, cancellationToken);
    }

    protected override void WriteWithoutResponseCore(byte[] bytes)
    {
        _ = Task.Run(() => _characteristic.WriteAsync(_gattClient, bytes, default));
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state, Action<TState, byte[]> onNotify, CancellationToken cancellationToken)
    {
        await _characteristic.EnableNotificationsAsync(_gattClient, bytes => onNotify(state, bytes), cancellationToken)
            .ConfigureAwait(false);
        return Disposable.Empty;
    }

    protected override Task DisableNotificationsAsync() => _characteristic.DisableNotificationsAsync(_gattClient);
}