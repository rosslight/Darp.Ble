using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    private readonly Subject<Unit> _whenDisconnectedSubject = new();
    private int _isDisconnected;

    /// <inheritdoc />
    public override bool IsConnected => Interlocked.CompareExchange(ref _isDisconnected, 0, 0) == 0;

    /// <inheritdoc />
    public override IObservable<Unit> WhenDisconnected => _whenDisconnectedSubject.AsObservable();

    /// <summary> Triggers the disconnect event </summary>
    internal void OnDisconnected()
    {
        if (Interlocked.Exchange(ref _isDisconnected, 1) == 1)
            return;
        _whenDisconnectedSubject.OnNext(Unit.Default);
        _whenDisconnectedSubject.OnCompleted();
    }

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
