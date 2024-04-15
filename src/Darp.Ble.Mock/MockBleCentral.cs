using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Logger;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

public sealed class MockBleCentral(BleDevice device, MockBlePeripheral peripheralMock, IObserver<LogEvent>? logger)
    : BleCentral(device, logger)
{
    private readonly MockBlePeripheral _peripheralMock = peripheralMock;

    protected override IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<IGattServerPeer>(observer =>
        {
            IMockBleConnection connection = new MockBleConnection();
            var mockDevice = new MockGattServerPeer(connection);
            _peripheralMock.OnCentralConnection(connection);
            observer.OnNext(mockDevice);
            return Disposable.Empty;
        });
    }
}