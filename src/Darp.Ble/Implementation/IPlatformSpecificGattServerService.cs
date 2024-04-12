using Darp.Ble.Data;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattServerService
{
    BleUuid Uuid { get; }
    Task DiscoverCharacteristicsAsync(CancellationToken cancellationToken);
    Task<IPlatformSpecificGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken);
}