using Darp.Ble.Gatt.Server;

namespace Darp.Ble;

public interface IBlePeripheral
{
    void AddService(GattServerService service);
}