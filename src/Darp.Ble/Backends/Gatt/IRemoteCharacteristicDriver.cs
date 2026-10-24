using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A characteristic discovered on a peripheral. Belongs to its connection </summary>
public interface IRemoteCharacteristicDriver
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    GattProperty Properties { get; }
}
