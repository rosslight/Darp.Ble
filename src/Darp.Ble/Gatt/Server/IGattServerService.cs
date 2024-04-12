using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

public interface IGattServerService
{
    BleUuid Uuid { get; }
}