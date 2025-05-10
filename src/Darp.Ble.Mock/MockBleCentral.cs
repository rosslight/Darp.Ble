using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Mock.Gatt;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

internal sealed class MockBleCentral(MockBleDevice device, ILogger<MockBleCentral> logger) : BleCentral(device, logger)
{
    private readonly MockBleDevice _device = device;

    /// <inheritdoc />
    protected override IObservable<GattServerPeer> ConnectToPeripheralCore(
        BleAddress address,
        BleConnectionParameters connectionParameters,
        BleObservationParameters observationParameters
    )
    {
        MockedBleDevice? peerDevice = _device.MockedDevices.FirstOrDefault(x => x.RandomAddress == address);
        if (peerDevice is null)
            return Observable.Throw<GattServerPeer>(
                new Exception($"Mock does not contain a device with address {address}")
            );
        return Observable.Create<GattServerPeer>(observer =>
        {
            MockGattClientPeer clientPeer = peerDevice.Peripheral.OnCentralConnection(address);
            // TODO _peripheralMock.StopAll();
            var mockDevice = new MockGattServerPeer(
                this,
                address,
                clientPeer,
                ServiceProvider.GetLogger<MockGattServerPeer>()
            );
            observer.OnNext(mockDevice);
            return Disposable.Empty;
        });
    }
}
