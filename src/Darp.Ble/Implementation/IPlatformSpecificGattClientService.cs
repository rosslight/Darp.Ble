using Darp.Ble.Data;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattClientService
{
    Task<IPlatformSpecificGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken);
}