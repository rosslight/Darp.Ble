using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Logger;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock;

internal sealed class MockBleCentral(BleDevice device, MockBlePeripheral peripheralMock, IObserver<LogEvent>? logger)
    : BleCentral(device, logger)
{
    private readonly MockBlePeripheral _peripheralMock = peripheralMock;

    /// <inheritdoc />
    protected override IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<IGattServerPeer>(observer =>
        {
            MockGattClientPeer clientPeer = _peripheralMock.OnCentralConnection(address);
            _peripheralMock.Stop();
            var mockDevice = new MockGattServerPeer(address, clientPeer);
            observer.OnNext(mockDevice);
            return Disposable.Empty;
        });
    }
}