using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> The client service </summary>
public interface IGattClientService : IGattAttribute
{
    /// <summary> The peripheral device this service was added to</summary>
    public IBlePeripheral Peripheral { get; }

    /// <summary> The UUID of the client service </summary>
    BleUuid Uuid { get; }

    /// <summary> The type of the service </summary>
    GattServiceType Type { get; }

    /// <summary> All characteristics of the client service </summary>
    IReadOnlyCollection<IGattClientCharacteristic> Characteristics { get; }

    /// <summary> Add a characteristic to the service </summary>
    /// <param name="uuid"> The UUID of the service to add </param>
    /// <param name="gattProperty"> The property of the service to add </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <returns> An <see cref="IGattClientCharacteristic"/> </returns>
    IGattClientCharacteristic AddCharacteristic(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite
    );
}
