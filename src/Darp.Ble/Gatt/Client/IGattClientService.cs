using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public interface IGattClientService
{
    BleUuid Uuid { get; }
    IReadOnlyDictionary<BleUuid, IGattClientCharacteristic> Characteristics { get; }

    Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid, GattProperty property, CancellationToken cancellationToken);
}