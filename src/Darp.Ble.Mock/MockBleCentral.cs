using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

public sealed class MockBleCentral(MockBlePeripheral peripheralMock) : IPlatformSpecificBleCentral
{
    private readonly MockBlePeripheral _peripheralMock = peripheralMock;

    public IObservable<GattServerPeer> ConnectToPeripheral(BleAddress address, BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        var gattClientPeer = new GattClientPeer(null!);
        var mockDevice = new MockGattServerPeer(_peripheralMock, gattClientPeer);
        var gattServerPeer = new GattServerPeer(mockDevice, isAlreadyConnected: true);
        _peripheralMock.OnNextConnected(gattClientPeer);
        return Observable.Return(gattServerPeer);
    }
}