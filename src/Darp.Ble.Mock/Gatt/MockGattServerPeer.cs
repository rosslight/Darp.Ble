using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerPeer : IPlatformSpecificGattServerPeer
{
    private readonly MockGattClientPeer _clientPeer;
    private readonly IMockBleConnection _connection;

    internal MockGattServerPeer(MockGattClientPeer clientPeer)
    {
        _clientPeer = clientPeer;
        _connection = connection;
    }

    public IObservable<ConnectionStatus> WhenConnectionStatusChanged => _connection.WhenConnectionStatusChanged;
    public IObservable<IPlatformSpecificGattServerService> DiscoverServices() => _clientPeer.GetServicesAsync();
    public IObservable<IPlatformSpecificGattServerService> DiscoverService(BleUuid uuid) => _connection.GetServiceAsync(uuid);
    public ValueTask DisposeAsync() => _connection.DisconnectAsync();

    public IObservable<Unit> WhenDisconnected => _connection.WhenConnectionStatusChanged
        .Where(x => x is ConnectionStatus.Disconnected)
        .Select(_ => Unit.Default);
}

public sealed class MockGattClientPeer : IGattClientPeer
{
    private readonly IMockBleConnection _connection;

    public MockGattClientPeer(BleAddress address, IPlatformSpecificGattServerPeer serverPeer)
    {
        serverPeer.
        _connection.WhenService.
        Address = address;
    }

    public bool IsConnected => true;
    public IObservable<Unit> WhenDisconnected => _connection.WhenConnectionStatusChanged
        .Where(x => x is ConnectionStatus.Disconnected)
        .Select(_ => Unit.Default);
    public BleAddress Address { get; }

    internal IObservable<IPlatformSpecificGattServerService> GetServicesAsync()
    {
        return Observable.Return()
    }
}