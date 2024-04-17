using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;
using Darp.Ble.WinRT.Gatt;
using Darp.Ble.WinRT.Gatt.Server;

namespace Darp.Ble.WinRT;

/// <inheritdoc />
public sealed class WinBleCentral(BleDevice device, IObserver<LogEvent>? logger) : BleCentral(device, logger)
{
    /// <inheritdoc />
    protected override IObservable<IGattServerPeer> ConnectToPeripheralCore(BleAddress address,
        BleConnectionParameters connectionParameters,
        BleScanParameters scanParameters)
    {
        return Observable.Create<IGattServerPeer>(async (observer, cancellationToken) =>
        {
            BluetoothLEDevice? winDev = await BluetoothLEDevice.FromBluetoothAddressAsync(address.Value, address.Type switch
            {
                BleAddressType.Public => BluetoothAddressType.Public,
                BleAddressType.NotAvailable => BluetoothAddressType.Unspecified,
                _ => BluetoothAddressType.Random,
            }).AsTask(cancellationToken);
            if (winDev is null)
            {
                observer.OnError(new Exception("PeripheralConnection: Failed!"));
                return;
            }
            observer.OnNext(new WinGattServerPeer(winDev));
        });
    }
}