using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleCentral(BleDevice device, MockBlePeripheral peripheralMock, ILogger? logger)
    : BleCentral(device, logger)
{
    private readonly MockBlePeripheral _peripheralMock = peripheralMock;

    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<GattServerPeer>(observer =>
        {
            MockGattClientPeer clientPeer = _peripheralMock.OnCentralConnection(address);
            // TODO _peripheralMock.StopAll();
            var mockDevice = new MockGattServerPeer(this, address, clientPeer, Logger);
            observer.OnNext(mockDevice);
            return Disposable.Empty;
        });
    }
}