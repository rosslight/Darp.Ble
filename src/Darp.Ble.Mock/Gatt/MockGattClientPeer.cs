using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattClientPeer(
    MockedBlePeripheral peripheral,
    BleAddress address,
    ILogger<MockGattClientPeer> logger
) : GattClientPeer(peripheral, address, logger)
{
    /// <inheritdoc />
    public override bool IsConnected => true;

    /// <inheritdoc />
    public override IObservable<Unit> WhenDisconnected => Observable.Empty<Unit>();

    public IObservable<IGattServerService> GetServices(MockGattServerPeer serverPeer)
    {
        return Peripheral
            .Services.Select(clientService => new MockGattServerService(
                this,
                serverPeer,
                (MockGattClientService)clientService,
                ServiceProvider.GetLogger<MockGattServerService>()
            ))
            .ToArray()
            .ToObservable();
    }

    public IObservable<IGattServerService> GetService(MockGattServerPeer serverPeer, BleUuid uuid)
    {
        return GetServices(serverPeer).Where(x => x.Uuid == uuid);
    }
}
