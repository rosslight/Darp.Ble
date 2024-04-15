using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Logger;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;
/*
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
}*/

public sealed class MockBlePeripheral(MockBleDevice device, IObserver<LogEvent>? logger) : BlePeripheral(device, logger)
{
    protected override Task<IGattClientService> CreateServiceAsyncCore(BleUuid uuid, CancellationToken cancellationToken)
    {
        var service = new MockGattClientService(uuid);
        return Task.FromResult<IGattClientService>(service);
    }

    public MockGattClientPeer OnCentralConnection(BleAddress address)
    {
        var clientPeer = new MockGattClientPeer(address, this);
        OnConnectedCentral(clientPeer);
        return clientPeer;
    }
}

public sealed class MockGattClientService(BleUuid uuid) : GattClientService(uuid)
{
    protected override Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid,
        GattProperty gattProperty,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IGattClientCharacteristic>(new MockGattClientCharacteristic(uuid, gattProperty));
    }
}
