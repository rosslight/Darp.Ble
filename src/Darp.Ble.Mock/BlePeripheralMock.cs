using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock;

public sealed class BlePeripheralMock : IBlePeripheral
{
    private List<GattServerService> _services = new();
    public void AddService(GattServerService service) => _services.Add(service);

    public void OnClientConnection(Action<IGattClientDevice> callback)
    {
        
    }
}