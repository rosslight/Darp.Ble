using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Implementation;

public interface IPlatformSpecificGattClientService
{
    Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken);
}