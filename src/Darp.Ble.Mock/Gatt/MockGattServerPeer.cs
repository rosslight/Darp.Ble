using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerPeer : GattServerPeer
{
    private readonly MockGattClientPeer _clientPeer;

    public MockGattServerPeer(BleAddress address, MockGattClientPeer clientPeer) : base(address)
    {
        _clientPeer = clientPeer;
    }

    public override IObservable<ConnectionStatus> WhenConnectionStatusChanged => Observable.Empty<ConnectionStatus>();
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices();
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(uuid);
}

internal sealed class MockGattClientPeer : IGattClientPeer
{
    private readonly Dictionary<BleUuid, IGattServerService> _services;

    public MockGattClientPeer(BleAddress address, MockBlePeripheral peripheral)
    {
        Address = address;
        _services = peripheral.Services
            .Select(x => (x.Key, new MockGattServerService(x.Key, (MockGattClientService)x.Value, this)))
            .ToDictionary(x => x.Key, x => (IGattServerService)x.Item2);
    }

    public IReadOnlyDictionary<BleUuid, IGattServerService> Services => _services;

    public bool IsConnected => true;
    public IObservable<Unit> WhenDisconnected => Observable.Empty<Unit>();
    public BleAddress Address { get; }

    public IObservable<IGattServerService> GetServices() => Services.Values.ToObservable();

    public IObservable<IGattServerService> GetService(BleUuid uuid)
    {
        return GetServices().Where(x => x.Uuid == uuid);
    }
}