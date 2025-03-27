using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockedBlePeripheral(MockedBleDevice device, ILogger<MockedBlePeripheral> logger)
    : BlePeripheral(device, logger)
{
    private readonly MockedBleDevice _device = device;

    public MockGattClientPeer OnCentralConnection(BleAddress address)
    {
        var clientPeer = new MockGattClientPeer(this, address, ServiceProvider.GetLogger<MockGattClientPeer>());
        OnConnectedCentral(clientPeer);
        return clientPeer;
    }

    /// <inheritdoc />
    protected override GattClientService AddServiceCore(BleUuid uuid, bool isPrimary)
    {
        return new MockGattClientService(
            uuid,
            isPrimary ? GattServiceType.Primary : GattServiceType.Secondary,
            this,
            ServiceProvider.GetLogger<MockGattClientService>()
        );
    }
}
