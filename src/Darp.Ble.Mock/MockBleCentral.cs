using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

public sealed class MockBleCentral(MockBlePeripheral peripheralMock) : IPlatformSpecificBleCentral
{
    private readonly MockBlePeripheral _peripheralMock = peripheralMock;

    public IObservable<(IPlatformSpecificGattServerPeer, ConnectionStatus)> ConnectToPeripheral(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<(IPlatformSpecificGattServerPeer, ConnectionStatus)>(observer =>
        {
            IMockBleConnection connection = new MockBleConnection();
            var mockDevice = new MockGattServerPeer(connection);
            _peripheralMock.OnCentralConnection(connection);
            observer.OnNext((mockDevice, ConnectionStatus.Connected));
            return Disposable.Empty;
        });
    }
}