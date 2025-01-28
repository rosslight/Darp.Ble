using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerService(
    MockGattClientPeer clientPeer,
    MockGattServerPeer serverPeer,
    MockGattClientService clientService,
    ILogger<MockGattServerService> logger) : GattServerService(serverPeer, clientService.Uuid, logger)
{
    private readonly MockGattClientService _clientService = clientService;
    public MockGattClientPeer GattClient { get; } = clientPeer;

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsCore() => _clientService
        .Characteristics
        .ToObservable()
        .Where(x => x is MockGattClientCharacteristic)
        .Select(x => new MockGattServerCharacteristic(this, x.Uuid, (MockGattClientCharacteristic)x, GattClient, LoggerFactory.CreateLogger<MockGattServerCharacteristic>()));

    /// <inheritdoc />
    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsCore(BleUuid uuid)
    {
        return DiscoverCharacteristicsCore().Where(x => x.Uuid == uuid);
    }
}