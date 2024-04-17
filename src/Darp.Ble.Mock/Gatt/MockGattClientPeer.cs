using System.Reactive;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Mock.Gatt;

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

    /// <inheritdoc />
    public bool IsConnected => true;
    /// <inheritdoc />
    public IObservable<Unit> WhenDisconnected => Observable.Empty<Unit>();
    /// <inheritdoc />
    public BleAddress Address { get; }

    public IObservable<IGattServerService> GetServices() => Services.Values.ToObservable();

    public IObservable<IGattServerService> GetService(BleUuid uuid)
    {
        return GetServices().Where(x => x.Uuid == uuid);
    }
}