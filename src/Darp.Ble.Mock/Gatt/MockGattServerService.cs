using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerService(BleUuid uuid, IGattClientService service) : IPlatformSpecificGattServerService
{
    private readonly Dictionary<BleUuid, MockGattServerCharacteristic> _characteristics = service.Characteristics
        .Select(x => (x.Key, new MockGattServerCharacteristic(x.Key, x.Value)))
        .ToDictionary();

    public BleUuid Uuid { get; } = uuid;

    public Task DiscoverCharacteristicsAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IPlatformSpecificGattServerCharacteristic?> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken)
    {
        return Task.FromResult<IPlatformSpecificGattServerCharacteristic?>(_characteristics.GetValueOrDefault(uuid));
    }
}