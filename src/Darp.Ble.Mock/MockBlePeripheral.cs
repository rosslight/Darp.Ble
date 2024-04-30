using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBlePeripheral(MockBleDevice device, MockBleBroadcaster broadcaster, ILogger? logger)
    : BlePeripheral(device, logger)
{
    private readonly MockBleBroadcaster _broadcaster = broadcaster;

    public MockGattClientPeer OnCentralConnection(BleAddress address)
    {
        var clientPeer = new MockGattClientPeer(address, this);
        OnConnectedCentral(clientPeer);
        return clientPeer;
    }

    /// <inheritdoc />
    protected override Task<IGattClientService> AddServiceAsyncCore(BleUuid uuid, CancellationToken cancellationToken)
    {
        var service = new MockGattClientService(uuid, this);
        return Task.FromResult<IGattClientService>(service);
    }

    /// <inheritdoc />
    public IDisposable Advertise(AdvertisingSet advertisingSet) => _broadcaster.Advertise(advertisingSet);

    /// <inheritdoc />
    public IDisposable Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters = null)
    {
        return _broadcaster.Advertise(source, parameters);
    }

    /// <inheritdoc />
    public void StopAll() => _broadcaster.StopAll();
}