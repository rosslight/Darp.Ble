using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A service the adapter hosts; centrals can discover it </summary>
public sealed class LocalGattService
{
    internal LocalGattService(BleUuid uuid)
    {
        Uuid = uuid;
    }

    /// <summary> The UUID of the service </summary>
    public BleUuid Uuid { get; }
}
