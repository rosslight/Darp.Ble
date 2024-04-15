using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

public interface IMockBleConnection
{
    IObservable<IPlatformSpecificGattServerService> GetServicesAsync();
    IObservable<IPlatformSpecificGattServerService> GetServiceAsync(BleUuid uuid);
    IObservable<Unit> WhenServices { get; }
    IObservable<BleUuid> WhenService { get; }
    Task GetCharacteristicAsync(BleUuid uuid);
    IObservable<BleUuid> WhenCharacteristic { get; }
    Task WriteAsync(byte[] bytes, CancellationToken cancellationToken);
    IObservable<byte[]> WhenWrite { get; }

    Task NotifyAsync(byte[] bytes, CancellationToken cancellationToken);
    IObservable<byte[]> WhenNotify { get; }
    IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }
    ValueTask DisconnectAsync();
}

public sealed class MockBleConnection : IMockBleConnection
{
    public IObservable<IPlatformSpecificGattServerService> GetServicesAsync()
    {
        throw new NotImplementedException();
    }

    public IObservable<IPlatformSpecificGattServerService> GetServiceAsync(BleUuid uuid)
    {
        throw new NotImplementedException();
    }

    public IObserver<BleUuid> WhenService { get; }
    public Task GetCharacteristicAsync(BleUuid uuid)
    {
        throw new NotImplementedException();
    }

    public IObservable<BleUuid> WhenCharacteristic { get; }
    public Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IObservable<byte[]> WhenWrite { get; }
    public Task NotifyAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IObservable<byte[]> WhenNotify { get; }
    public IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }
    public ValueTask DisconnectAsync()
    {
        throw new NotImplementedException();
    }
}

public sealed class MockBlePeripheral : IPlatformSpecificBlePeripheral
{
    private readonly Dictionary<BleAddress, IGattClientPeer> _clients = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();

    public Task<IPlatformSpecificGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken)
    {
        var specificService = new MockGattClientService();
        return Task.FromResult<IPlatformSpecificGattClientService>(specificService);
    }

    public IObservable<IGattClientPeer> WhenConnected => _whenConnected.AsObservable();

    public void OnCentralConnection(IMockBleConnection connection)
    {
        var clientPeer = new MockGattClientPeer(connection);
        _whenConnected.OnNext(clientPeer);
        _clients[clientPeer.Address] = clientPeer;
    }
}

public sealed class MockGattClientService : IPlatformSpecificGattClientService
{
    public Task<IPlatformSpecificGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid,
        GattProperty gattProperty,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IPlatformSpecificGattClientCharacteristic>(
            new MockGattClientCharacteristic(gattProperty));
    }
}
