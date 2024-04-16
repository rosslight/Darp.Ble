using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerService : IAsyncDisposable
{
    BleUuid Uuid { get; }
    Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default);
    IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics { get; }
}