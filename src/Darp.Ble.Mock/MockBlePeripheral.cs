using System.Reactive;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Mock;

public sealed class MockBlePeripheral : IBlePeripheral
{
    private readonly Dictionary<BleUuid, IGattClientService> _services = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();
    private readonly Subject<Unit> _whenDisconnected = new();
    public IReadOnlyDictionary<BleUuid, IGattClientService> Services => _services;
    public void AddService(IGattClientService service) => _services[service.Uuid] = service;
    public IObservable<IGattClientPeer> WhenConnected => _whenConnected;
    public IObservable<Unit> WhenDisconnected => _whenDisconnected;

    public void OnNextConnected(IGattClientPeer clientPeer)
    {
        _whenConnected.OnNext(clientPeer);
        clientPeer.WhenDisconnected.Subscribe(x => _whenDisconnected.OnNext(x));
    }
}