using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

public sealed class MockBleCentral(BlePeripheralMock peripheralMock) : IPlatformSpecificBleCentral
{
    private readonly BlePeripheralMock _peripheralMock = peripheralMock;

    public IObservable<GattServerDevice> ConnectToPeripheral(BleAddress address, BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        var mockDevice = new MockGattServerDevice(_peripheralMock);
        return Observable.Return(new GattServerDevice(mockDevice, isAlreadyConnected: true));
    }
}