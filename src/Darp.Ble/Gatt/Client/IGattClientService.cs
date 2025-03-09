using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;

namespace Darp.Ble.Gatt.Client;

/// <summary> The client service </summary>
public interface IGattClientService
{
    /// <summary> The peripheral device this service was added to</summary>
    public IBlePeripheral Peripheral { get; }

    /// <summary> The UUID of the client service </summary>
    BleUuid Uuid { get; }

    /// <summary> The type of the service </summary>
    GattServiceType Type { get; }

    /// <summary> Get the service declaration </summary>
    internal IGattAttribute Declaration { get; }

    /// <summary> All characteristics of the client service </summary>
    IReadOnlyCollection<IGattClientCharacteristic> Characteristics { get; }

    /// <summary> Add a characteristic to the service </summary>
    /// <param name="properties"> The properties of the characteristic to create </param>
    /// <param name="value"> The characteristic value </param>
    /// <param name="descriptors"> A collection of descriptors to add when the characteristic was added </param>
    /// <returns> An <see cref="IGattClientCharacteristic"/> </returns>
    IGattClientCharacteristic AddCharacteristic(
        GattProperty properties,
        IGattCharacteristicValue value,
        IGattCharacteristicValue[] descriptors
    );
}
