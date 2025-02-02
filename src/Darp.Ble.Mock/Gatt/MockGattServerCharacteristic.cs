using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerCharacteristic(MockGattServerService service,
    BleUuid uuid,
    MockGattClientCharacteristic characteristic,
    MockGattClientPeer gattClient,
    ILogger<MockGattServerCharacteristic> logger)
    : GattServerCharacteristic(service, characteristic.AttributeHandle, uuid, characteristic.Properties, logger)
{
    private readonly MockGattClientCharacteristic _characteristic = characteristic;
    private readonly MockGattClientPeer _gattClient = gattClient;

    protected override IObservable<IGattServerDescriptor> DiscoverDescriptorsCore() => _characteristic
        .Descriptors
        .ToObservable()
        .Where(x => x is MockGattClientDescriptor)
        .Select(x => new MockGattServerDescriptor(this,
            x.Key,
            (MockGattClientDescriptor)x.Value,
            LoggerFactory.CreateLogger<MockGattServerDescriptor>()));

    /// <inheritdoc />
    protected override Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        return _characteristic.WriteAsync(_gattClient, bytes, cancellationToken);
    }

    protected override void WriteWithoutResponseCore(byte[] bytes)
    {
        _ = Task.Run(() => _characteristic.WriteAsync(_gattClient, bytes, CancellationToken.None));
    }

    protected override async Task<byte[]> ReadAsyncCore(CancellationToken cancellationToken)
    {
        return await _characteristic.GetValueAsync(_gattClient, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state, Action<TState, byte[]> onNotify, CancellationToken cancellationToken)
    {
        await _characteristic.EnableNotificationsAsync(_gattClient, bytes => onNotify(state, bytes), cancellationToken)
            .ConfigureAwait(false);
        return Disposable.Empty;
    }

    protected override Task DisableNotificationsAsync() => _characteristic.DisableNotificationsAsync(_gattClient);
}