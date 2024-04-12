using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattServerDevice : IPlatformSpecificGattServerDevice
{
    private readonly BlePeripheralMock _peripheralMock;

    internal MockGattServerDevice(BlePeripheralMock peripheralMock)
    {
        _peripheralMock = peripheralMock;
        WhenConnectionStatusChanged = Observable.Never<ConnectionStatus>();
    }
    public IObservable<ConnectionStatus> WhenConnectionStatusChanged { get; }

    public IObservable<IPlatformSpecificGattServerService> DiscoverServices()
    {
        throw new NotImplementedException();
    }

    public IObservable<IPlatformSpecificGattServerService> DiscoverService(BleUuid uuid)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}