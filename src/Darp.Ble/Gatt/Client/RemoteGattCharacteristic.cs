using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic discovered on a connected peripheral </summary>
public sealed class RemoteGattCharacteristic
{
    internal RemoteGattCharacteristic(IRemoteCharacteristicDriver driver)
    {
        Uuid = driver.Uuid;
        Properties = driver.Properties;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties { get; }
}
