using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerService(BleUuid uuid, MockGattClientService service) : GattServerService(uuid)
{
    private readonly Dictionary<BleUuid, MockGattServerCharacteristic> _characteristics = service.Characteristics
        .Select(x => (x.Key, new MockGattServerCharacteristic(x.Key, new MockGattClientCharacteristic(x.Key, x.Value.Property))))
        .ToDictionary();

    public BleUuid Uuid { get; } = uuid;

    protected override Task DiscoverCharacteristicsAsyncCore(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task<IGattServerCharacteristic?> DiscoverCharacteristicAsyncCore(BleUuid uuid, CancellationToken cancellationToken)
    {
        return Task.FromResult<IGattServerCharacteristic?>(_characteristics.GetValueOrDefault(uuid));
    }
}