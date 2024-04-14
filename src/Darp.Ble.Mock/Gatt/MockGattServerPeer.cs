using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerPeer : IPlatformSpecificGattServerPeer
{
    private readonly Dictionary<BleUuid, MockGattServerService> _services;
    private readonly Subject<Unit> _whenDisconnected = new();

    internal MockGattServerPeer(MockBlePeripheral peripheralMock, GattClientPeer gattClientPeer)
    {
        _services = peripheralMock.Services
            .Select(x => (x.Key, new MockGattServerService(x.Key, x.Value)))
            .ToDictionary();
        WhenConnectionStatusChanged = _whenDisconnected.Select(_ => ConnectionStatus.Disconnected);
    }

    public IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }

    public IObservable<IPlatformSpecificGattServerService> DiscoverServices() => _services
        .Select(x => x.Value)
        .ToObservable();

    public IObservable<IPlatformSpecificGattServerService> DiscoverService(BleUuid uuid) => _services
        .Where(x => x.Key == uuid)
        .Select(x => x.Value)
        .ToObservable();

    public ValueTask DisposeAsync()
    {
        _whenDisconnected.OnNext(Unit.Default);
        return ValueTask.CompletedTask;
    }

    public IObservable<Unit> WhenDisconnected => _whenDisconnected;
}