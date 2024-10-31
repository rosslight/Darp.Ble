using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The gatt server service interface </summary>
public interface IGattServerService
{
    /// <summary> The service uuid </summary>
    BleUuid Uuid { get; }
    /// <summary> All discovered characteristics </summary>
    IReadOnlyDictionary<BleUuid, IGattServerCharacteristic> Characteristics { get; }
    /// <summary> Discover a characteristic with a given <paramref name="uuid"/> </summary>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The discovered gatt server characteristic </returns>
    Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default);
}