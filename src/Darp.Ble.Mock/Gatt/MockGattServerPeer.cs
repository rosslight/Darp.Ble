using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerPeer : GattServerPeer
{
    private readonly MockGattClientPeer _clientPeer;

    public MockGattServerPeer(BleAddress address, MockGattClientPeer clientPeer) : base(address)
    {
        _clientPeer = clientPeer;
    }

    public override IObservable<ConnectionStatus> WhenConnectionStatusChanged => Observable.Empty<ConnectionStatus>();
    protected override IObservable<IGattServerService> DiscoverServicesCore() => _clientPeer.GetServices();
    protected override IObservable<IGattServerService> DiscoverServiceCore(BleUuid uuid) => _clientPeer.GetService(uuid);
    public IObservable<Unit> WhenDisconnected => Observable.Empty<Unit>();
}

public sealed class MockGattClientPeer : IGattClientPeer
{
    private readonly MockBlePeripheral _peripheral;

    public MockGattClientPeer(BleAddress address, MockBlePeripheral peripheral)
    {
        Address = address;
        _peripheral = peripheral;
    }

    public bool IsConnected => true;
    public IObservable<Unit> WhenDisconnected => Observable.Empty<Unit>();
    public BleAddress Address { get; }

    internal IObservable<IGattServerService> GetServices()
    {
        return _peripheral.Services.ToObservable()
            .Select(x => new MockGattServerService(x.Key, new MockGattClientService(x.Key)));
    }
    internal IObservable<IGattServerService> GetService(BleUuid uuid)
    {
        return GetServices().Where(x => x.Uuid == uuid);
    }
}