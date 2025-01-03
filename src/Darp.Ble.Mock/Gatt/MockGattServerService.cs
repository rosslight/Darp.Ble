using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerService(
    BleUuid uuid,
    MockGattClientService service,
    MockGattClientPeer gattClient,
    ILogger? logger) : GattServerService(uuid, logger)
{
    public MockGattClientPeer GattClient { get; } = gattClient;
    private readonly MockGattClientService _service = service;

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsAsyncCore() => _service
        .Characteristics
        .ToObservable()
        .Where(x => x.Value is MockGattClientCharacteristic)
        .Select(x => new MockGattServerCharacteristic(x.Key, (MockGattClientCharacteristic)x.Value, GattClient));

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicAsyncCore(BleUuid uuid)
    {
        return DiscoverCharacteristicsAsyncCore().Where(x => x.Uuid == uuid);
    }
}