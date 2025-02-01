using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> The gatt server service interface </summary>
public interface IGattServerService
{
    /// <summary> The peer device this service was discovered from </summary>
    IGattServerPeer Peer { get; }

    /// <summary> The service uuid </summary>
    BleUuid Uuid { get; }
    /// <summary> The type of the service </summary>
    GattServiceType Type { get; }

    /// <summary> All discovered characteristics </summary>
    IReadOnlyCollection<IGattServerCharacteristic> Characteristics { get; }

    /// <summary> Discover all characteristics. Check <see cref="Characteristics"/> for the available characteristics </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task that completes when all characteristics are discovered </returns>
    Task DiscoverCharacteristicsAsync(CancellationToken cancellationToken = default);
    /// <summary> Discover a characteristic with a given <paramref name="uuid"/> </summary>
    /// <param name="uuid"> The characteristic uuid to be discovered </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The discovered gatt server characteristic </returns>
    Task<IGattServerCharacteristic> DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken = default);
}