using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> The client service </summary>
public interface IGattClientService
{
    /// <summary> The UUID of the client service </summary>
    BleUuid Uuid { get; }
    /// <summary> All characteristics of the client service </summary>
    IReadOnlyDictionary<BleUuid, IGattClientCharacteristic> Characteristics { get; }

    /// <summary> Add a characteristic to the service </summary>
    /// <param name="uuid"> The UUID of the service to add </param>
    /// <param name="property"> The property of the service to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> An <see cref="IGattClientCharacteristic"/> </returns>
    Task<IGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        GattProperty property,
        Func<IGattClientPeer, CancellationToken, ValueTask<byte[]>>? onRead,
        Func<IGattClientPeer, byte[], CancellationToken, ValueTask<GattProtocolStatus>>? onWrite,
        CancellationToken cancellationToken
    );
}